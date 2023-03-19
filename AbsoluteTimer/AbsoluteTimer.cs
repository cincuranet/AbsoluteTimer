using System;
using System.Runtime.InteropServices;

namespace AbsoluteTimer
{
    public class AbsoluteTimer : IDisposable
    {
        private readonly IDisposable _timer;

        private static Action<object> WrapCallback(Action callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            return x => callback();
        }

        public AbsoluteTimer(DateTime dt, Action callback) : this(dt, WrapCallback(callback), null)
        {
        }

        public AbsoluteTimer(DateTime dt, Action<object> callback, object state)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _timer = new Win32.AbsoluteTimerWin32(dt, callback, state);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _timer = new Linux.AbsoluteTimerLinux(dt, callback, state);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
