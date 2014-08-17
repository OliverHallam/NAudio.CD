using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

namespace NAudio.CD
{
    internal class DriveLock : IDisposable
    {
        private readonly SafeFileHandle handle;

        public DriveLock(SafeFileHandle handle)
        {
            this.handle = handle;
        }

        ~DriveLock()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            this.Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!PreventMediaRemoval(this.handle, false))
            {
                if (disposing)
                {
                    // if this didn't work then there's not much we can do in a finalizer
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
            }
        }

        public static DriveLock Lock(SafeFileHandle handle)
        {
            if (!PreventMediaRemoval(handle, true))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            return new DriveLock(handle);
        }

        private static bool PreventMediaRemoval(SafeFileHandle handle, bool preventMediaRemoval)
        {
            var pmr = new NativeMethods.PREVENT_MEDIA_REMOVAL { PreventMediaRemoval = preventMediaRemoval ? (byte)1 : (byte)0 };
            uint dummy = 0;
            return NativeMethods.DeviceIoControl(
                handle,
                (uint)NativeMethods.IOControlCode.IOCTL_STORAGE_MEDIA_REMOVAL,
                ref pmr,
                (uint)Marshal.SizeOf(typeof(NativeMethods.PREVENT_MEDIA_REMOVAL)),
                IntPtr.Zero,
                0,
                ref dummy,
                IntPtr.Zero) != 0;
        }
    }
}
