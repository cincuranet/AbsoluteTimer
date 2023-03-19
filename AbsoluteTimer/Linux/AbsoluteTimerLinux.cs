using System;
using System.ComponentModel;

namespace AbsoluteTimer.Linux
{
    internal class AbsoluteTimerLinux : IDisposable
    {
        private readonly int _timerFD;

        private bool _disposedValue;
        private readonly object _disposeLocker = new object();

        public AbsoluteTimerLinux(DateTime dt, Action<object> callback, object state)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            _timerFD = Interop.TimerFD_Create(Interop.CLOCK_REALTIME, 0);
            if (_timerFD == -1)
            {
                throw new Win32Exception();
            }
            SetTimerTime(new DateTimeOffset(dt).ToUnixTimeSeconds());

            FDMonitor.Instance.Add(_timerFD, () => callback(state));
        }

        /// <param name="seconds">Seconds since 1970 when the timer should trigger. Zero to disarm timer.</param>
        private void SetTimerTime(long seconds)
        {
            if (Environment.Is64BitProcess)
            {
                var timeSpec = new Interop64.itimerspec()
                {
                    it_value = new Interop64.timespec()
                    {
                        tv_sec = seconds,
                    }
                };

                if (Interop64.TimerFD_SetTime(_timerFD, Interop.TFD_TIMER_ABSTIME, timeSpec, null) != 0)
                {
                    throw new Win32Exception();
                }
            }
            else
            {
                var timeSpec = new Interop32.itimerspec()
                {
                    it_value = new Interop32.timespec()
                    {
                        tv_sec = checked((int)seconds), // Going to break in January 2038
                    }
                };

                if (Interop32.TimerFD_SetTime(_timerFD, Interop.TFD_TIMER_ABSTIME, timeSpec, null) != 0)
                {
                    throw new Win32Exception();
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (_disposeLocker)
            {
                if (!_disposedValue)
                {
                    SetTimerTime(0);
                    FDMonitor.Instance.Cancel(_timerFD);

                    Interop.Close(_timerFD);
                    _disposedValue = true;
                }
            }
        }

        ~AbsoluteTimerLinux()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
