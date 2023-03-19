using System;
using System.Runtime.InteropServices;

namespace AbsoluteTimer.Win32
{
    static class Interop
    {
        public delegate void TimerCallback([In, Out] IntPtr Instance, [In, Out, Optional] IntPtr Context, [In, Out] IntPtr Timer);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateThreadpoolTimer([In] TimerCallback pfnti, [In, Out] IntPtr pv, [Optional] IntPtr pcbe);

        [DllImport("kernel32.dll")]
        public static extern void SetThreadpoolTimer([In, Out] IntPtr pti, [In, Optional] IntPtr pftDueTime, [In] uint msPeriod, [In, Optional] uint msWindowLength);

        [DllImport("kernel32.dll")]
        public static extern void CloseThreadpoolTimer([In, Out] IntPtr pti);
    }
}
