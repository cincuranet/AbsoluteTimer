using System;
using System.Runtime.InteropServices;

namespace AbsoluteTimer.Linux
{
    internal static class Interop
    {
        public const int CLOCK_REALTIME = 0x00000000;
        public const int TFD_TIMER_ABSTIME = 0x00000001;

        [DllImport("libc", EntryPoint = "poll", SetLastError = true)]
        public static extern int Poll([In, Out] pollfd[] fds, int fdsCount, int timeout);

        [DllImport("libc", EntryPoint = "timerfd_create", SetLastError = true)]
        public static extern int TimerFD_Create(int clockid, int flags);

        [DllImport("libc", EntryPoint = "close", SetLastError = true)]
        public static extern int Close(int fd);

        [Flags]
        public enum POLL_EVENTS : ushort
        {
            NONE = 0x0000,
            POLLIN = 0x001,
            POLLPRI = 0x002,
            POLLOUT = 0x004,
            POLLMSG = 0x400,
            POLLREMOVE = 0x1000,
            POLLRDHUP = 0x2000,
            // output only
            POLLERR = 0x008,
            POLLHUP = 0x010,
            POLLNVAL = 0x020
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct pollfd
        {
            public int fd;
            public POLL_EVENTS events;
            public POLL_EVENTS revents;
        }
    }
}
