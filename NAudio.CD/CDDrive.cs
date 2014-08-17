using System;
using System.IO;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

using NAudio.Wave;

namespace NAudio.CD
{
    public sealed class CDDrive : IDisposable
    {
        private readonly SafeFileHandle handle;

        private CDDrive(SafeFileHandle handle)
        {
            this.handle = handle;
        }

        public void Dispose()
        {
            if (this.handle != null)
                this.handle.Dispose();
        }

        public static CDDrive Open(char driveLetter)
        {
            var fileName = @"\\.\" + driveLetter + ":";

            var handle = NativeMethods.CreateFile(
                fileName,
                FileAccess.Read, 
                FileShare.ReadWrite, 
                IntPtr.Zero,
                FileMode.Open, 
                0,
                IntPtr.Zero);

            if (handle.IsInvalid)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            return new CDDrive(handle);
        }

        public CDTrack[] ReadTrackInfo()
        {
            using (DriveLock.Lock(this.handle))
            {
                NativeMethods.CDROM_TOC toc;
                uint bytesRead = 0;
                if (NativeMethods.DeviceIoControl(
                    this.handle,
                    (uint)NativeMethods.IOControlCode.IOCTL_CDROM_READ_TOC,
                    IntPtr.Zero,
                    0,
                    out toc,
                    (uint)Marshal.SizeOf(typeof(NativeMethods.CDROM_TOC)),
                    ref bytesRead,
                    IntPtr.Zero) == 0)
                {
                    throw new Exception();
                }

                var tracks = new CDTrack[toc.LastTrack];
                for (var i = toc.FirstTrack - 1; i < toc.LastTrack; i++)
                {
                    tracks[i] = new CDTrack(
                        this.AddressToSector(toc.TrackData[i].Address),
                        this.AddressToSector(toc.TrackData[i + 1].Address));
                }

                return tracks;
            }
        }

        public IWaveProvider ReadTrack(CDTrack track)
        {
            return new CdWaveProvider(this.handle, track.StartSector, track.EndSector);
        }

        private uint AddressToSector(byte[] address)
        {
            return address[1] * 4500u + address[2] * 75u + address[3];
        }
    }
}
