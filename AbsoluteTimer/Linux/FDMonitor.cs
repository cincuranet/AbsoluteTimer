using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AbsoluteTimer.Linux
{
    /// <summary>
    /// Runs a background thread to monitor all the timer file descriptors. Once created, the thread won't stop until the process ends.
    /// </summary>
    internal class FDMonitor
    {
        private static FDMonitor _instance;
        private static readonly object _creationLock = new object();

        public static FDMonitor Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                lock (_creationLock)
                {
                    if (_instance == null)
                    {
                        _instance = new FDMonitor();
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        /// Lock this before use.
        /// </summary>
        private readonly AnonymousPipeServerStream _pipeServer;

        private readonly Thread _thread;

        /// <summary>
        /// Key=File Descriptor, Value=Callback for when FD gets some data to read.
        /// </summary>
        private readonly ConcurrentDictionary<int, Action> _fileDescriptors = new ConcurrentDictionary<int, Action>();

        /// <summary>
        /// Lock this before use.
        /// </summary>
        private readonly List<ManualResetEventSlim> _pollExitNotifications = new List<ManualResetEventSlim>();

        public FDMonitor()
        {
            _pipeServer = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.None);

            _thread = new Thread(Loop)
            {
                Name = $"AbsoluteTimer - {nameof(FDMonitor)}",
                IsBackground = true
            };
            _thread.Start();
        }

        private static Interop.pollfd NewPollFD(int fd) => new Interop.pollfd()
        {
            fd = fd,
            events = Interop.POLL_EVENTS.POLLIN
        };

        private Interop.pollfd[] GetPollArray(int pipeFD)
        {
            var list = new List<Interop.pollfd>
            {
                NewPollFD(pipeFD)
            };

            list.AddRange(_fileDescriptors.Keys.Select(NewPollFD));

            return list.ToArray();
        }

        private AnonymousPipeClientStream MakePipeClient()
        {
            lock (_pipeServer)
            {
                return new AnonymousPipeClientStream(PipeDirection.In, _pipeServer.ClientSafePipeHandle);
            }
        }

        private void Loop()
        {
            using (var pipeClient = MakePipeClient())
            {
                var pipeClientFD = pipeClient.SafePipeHandle.DangerousGetHandle().ToInt32();

                while (true)
                {
                    var fds = GetPollArray(pipeClientFD);
                    var pollResult = Interop.Poll(fds, fds.Length, -1); // Block until either a timer is raised, or the pipe is written to.
                    if (pollResult < 0)
                    {
                        throw new Win32Exception();
                    }

                    lock (_pollExitNotifications)
                    {
                        foreach (var notification in _pollExitNotifications)
                        {
                            notification.Set();
                        }
                        _pollExitNotifications.Clear();
                    }

                    if (fds[0].revents.HasFlag(Interop.POLL_EVENTS.POLLIN))
                    {
                        pipeClient.ReadByte();
                    }

                    var hitTimerFDs = fds
                        .Skip(1) // Skip the pipe
                        .Where(x => x.revents.HasFlag(Interop.POLL_EVENTS.POLLIN))
                        .Select(x => x.fd);

                    foreach (var hitFD in hitTimerFDs)
                    {
                        if (_fileDescriptors.TryRemove(hitFD, out var callback))
                        {
                            Task.Run(callback); // Use the thread pool for the callbacks so our main thread can process other timers.
                        }
                    }
                }
            }
        }

        public void Add(int fd, Action callback)
        {
            if (!_fileDescriptors.TryAdd(fd, callback))
            {
                throw new InvalidOperationException("This FD is already being monitored.");
            }

            lock (_pipeServer)
            {
                _pipeServer.WriteByte(1);
            }
        }

        public bool Cancel(int fd)
        {
            bool removed = _fileDescriptors.TryRemove(fd, out _);
            if (removed)
            {
                var notification = new ManualResetEventSlim();

                lock (_pollExitNotifications)
                {
                    _pollExitNotifications.Add(notification);
                }

                lock (_pipeServer)
                {
                    _pipeServer.WriteByte(1);
                }

                notification.Wait(); // Don't return until we know that poll() has stopped using the fd. Note that _pipeServer.WaitForPipeDrain() is not supported on Linux so can't use that here.
                notification.Dispose(); // Not using a "using ()" statement here because if any of this fails then we would Dispose() something that the loop thread is going to try and use. Safer to just not call Dispose() in the unlikely event of an exception here.
            }
            return removed;
        }
    }
}
