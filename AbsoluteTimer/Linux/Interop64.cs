using System.Runtime.InteropServices;

namespace AbsoluteTimer.Linux
{
    internal static class Interop64
    {
        [DllImport("libc", EntryPoint = "timerfd_settime", SetLastError = true)]
        public static extern int TimerFD_SetTime(int fd, int flags, itimerspec newValue, [Out] itimerspec oldValue);

        [StructLayout(LayoutKind.Sequential)]
        public class timespec
        {
            /// <summary>
            /// Seconds
            /// </summary>
            public long tv_sec;

            /// <summary>
            /// Nanoseconds
            /// </summary>
            public long tv_nsec;
        };

        [StructLayout(LayoutKind.Sequential)]
        public class itimerspec
        {
            /// <summary>
            /// Interval for periodic timer
            /// </summary>
            public timespec it_interval;

            /// <summary>
            /// Initial expiration
            /// </summary>
            public timespec it_value;
        }
    }
}
