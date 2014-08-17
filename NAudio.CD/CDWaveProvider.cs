using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

using NAudio.Wave;

namespace NAudio.CD
{
    internal class CdWaveProvider : IWaveProvider
    {
        private const int SectorsToRead = 32;
        private const int SectorSize = 2048;
        private const int SectorAudioSize = 2352;

        private static readonly WaveFormat CDWaveFormat = new WaveFormat();

        private readonly SafeFileHandle handle;
        private uint currentSector;
        private readonly uint endSector;

        private readonly byte[] currentBlock = new byte[SectorsToRead * SectorAudioSize];
        private int currentBlockLength;
        private int currentBlockIndex;

        public CdWaveProvider(SafeFileHandle handle, uint startSector, uint endSector)
        {
            this.handle = handle;
            this.currentSector = startSector;
            this.endSector = endSector;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;

            if (this.currentBlockLength > 0)
            {
                var bytesCopied = this.ReadFromCurrentBlock(buffer, offset, count);
                count -= bytesCopied;
                offset += bytesCopied;
                totalBytesRead += bytesCopied;
            }

            while (count > SectorsToRead * SectorSize && this.currentSector != this.endSector)
            {
                var bytesRead = this.ReadSector(buffer, offset, count);
                count -= bytesRead;
                offset += bytesRead;
                totalBytesRead += bytesRead;
            }

            if (count > 0 && this.currentSector != this.endSector)
            {
                this.currentBlockLength = this.ReadSector(this.currentBlock, 0, this.currentBlock.Length);
                this.currentBlockIndex = 0;

                var bytesCopied = this.ReadFromCurrentBlock(buffer, offset, count);
                count -= bytesCopied;
                offset += bytesCopied;
                totalBytesRead += bytesCopied;
            }

            Debug.Assert(count == 0);
            return totalBytesRead;
        }

        private int ReadFromCurrentBlock(byte[] buffer, int offset, int count)
        {
            var bytesCopied = Math.Min(count, this.currentBlockLength - this.currentBlockIndex);
            Array.Copy(this.currentBlock, this.currentBlockIndex, buffer, offset, bytesCopied);
            this.currentBlockIndex += bytesCopied;

            if (this.currentBlockIndex == this.currentBlockLength)
            {
                this.currentBlockLength = 0;
                this.currentBlockIndex = -1;
            }
            return bytesCopied;
        }

        private unsafe int ReadSector(byte[] buffer, int offset, int count)
        {
            var sectorCount = Math.Min(SectorsToRead, this.currentSector - this.endSector);
            if (count < sectorCount)
                throw new Exception();

            var info = new NativeMethods.RAW_READ_INFO
                       {
                           DiskOffset = this.currentSector * SectorSize,
                           SectorCount = sectorCount,
                           TrackModeType = NativeMethods.TRACK_MODE_TYPE.CDDA
                       };
            int bytesRead = 0;

            fixed (byte* bufferPointer = &buffer[offset])
            {
                if (NativeMethods.DeviceIoControl(
                    this.handle,
                    (uint)NativeMethods.IOControlCode.IOCTL_CDROM_RAW_READ,
                    ref info,
                    Marshal.SizeOf(typeof(NativeMethods.RAW_READ_INFO)),
                    (IntPtr)bufferPointer,
                    count,
                    ref bytesRead,
                    IntPtr.Zero) == 0)
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
            }

            this.currentSector += sectorCount;
            return bytesRead;
        }

        public WaveFormat WaveFormat
        {
            get
            {
                return CDWaveFormat;
            }
        }
    }
}
