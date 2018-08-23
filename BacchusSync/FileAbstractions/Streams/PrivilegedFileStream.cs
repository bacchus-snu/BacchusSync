using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using pGina.Plugin.BacchusSync.Exceptions;
using pGina.Plugin.BacchusSync.Extra;

namespace pGina.Plugin.BacchusSync.FileAbstractions.Streams
{
    internal class PrivilegedFileStream : Stream
    {
        #region Windows API
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        private enum Access : UInt32
        {
            GenericWrite = 0x40000000,
            GenericRead = 0x80000000,
        }

        private enum FileFlags : UInt32
        {
            ReadOnly = 1,
            Hidden = 2,
            System = 4,
            Archive = 32,
            Normal = 128,
            Temporary = 256,
            Offline = 4096,
            Encrypted = 16384,
            BackupSemantics = 0x02000000,
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafeFileHandle CreateFile
        (
            [MarshalAs(UnmanagedType.LPTStr)] string fileName,
            [MarshalAs(UnmanagedType.U4)] Access desiredAccess,
            [MarshalAs(UnmanagedType.U4)] FileShare shareMode,
            IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
            [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
            [MarshalAs(UnmanagedType.U4)] FileFlags flagsAndAttributes,
            IntPtr templateFile
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetFileSizeEx(SafeFileHandle file, out long fileSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetFilePointerEx(SafeFileHandle file, long distanceToMove, out long filePointer, SeekOrigin moveMethod);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FlushFileBuffers(SafeFileHandle file);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern unsafe bool ReadFile(SafeFileHandle file, byte *buffer, uint numberOfBytesToRead, out uint numberOfBytesRead, NativeOverlapped *overlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetEndOfFile(SafeFileHandle file);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern unsafe bool WriteFile(SafeFileHandle file, byte *buffer, uint numberOfBytesToWrite, out uint numberOfBytesWritten, NativeOverlapped* lpOverlapped);
        #endregion

        private readonly SafeFileHandle fileHandle;
        private readonly bool canRead = false;
        private readonly bool canWrite = false;

        internal PrivilegedFileStream(string path, FileAccess fileAccess, FileShare share, FileMode mode)
        {
            Access access = 0;
            if (fileAccess.HasFlag(FileAccess.Read))
            {
                access |= Access.GenericRead;
                canRead = true;
            }
            if (fileAccess.HasFlag(FileAccess.Write))
            {
                access |= Access.GenericWrite;
                canWrite = true;
            }

            fileHandle = CreateFile(path, access, share, IntPtr.Zero, mode, FileFlags.Normal | FileFlags.BackupSemantics, IntPtr.Zero);
            if (fileHandle.DangerousGetHandle() == INVALID_HANDLE_VALUE)
            {
                throw new IOException("Cannot open PrivilegedFileStream : " + ApiUtils.LastError());
            }
        }

        public override bool CanRead => canRead;

        public override bool CanSeek => true;

        public override bool CanWrite => canWrite;

        public override long Length
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                if (GetFileSizeEx(fileHandle, out long fileSize))
                {
                    return fileSize;
                }
                else
                {
                    throw new IOException("Cannot get file size : " + ApiUtils.LastError());
                }
            }
        }

        public override long Position
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                if (SetFilePointerEx(fileHandle, 0, out long position, SeekOrigin.Current))
                {
                    return position;
                }
                else
                {
                    throw new IOException("SetFilePointerEx : " + ApiUtils.LastError());
                }
            }
            set
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                if (!SetFilePointerEx(fileHandle, value, out long position, SeekOrigin.Begin))
                {
                    throw new IOException("SetFilePointerEx : " + ApiUtils.LastError());
                }
            }
        }

        public override void Flush()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (!FlushFileBuffers(fileHandle))
            {
                throw new IOException("FlushFileBuffers : " + ApiUtils.LastError());
            }
        }

        public override unsafe int Read(byte[] buffer, int offset, int count)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
            else if (!canRead)
            {
                throw new NotSupportedException("The file was not opened to read.");
            }
            else if (buffer == null)
            {
                throw new ArgumentNullException("buffer is null.");
            }
            else if (offset + count > buffer.Length)
            {
                throw new ArgumentException("The sum of offset and buffer is larger than the buffer length.");
            }
            else if (offset < 0 || count < 0)
            {
                throw new ArgumentOutOfRangeException("offset or count is negative.");
            }
            else
            {
                fixed (byte *pointerToBuffer = buffer)
                {
                    if (ReadFile(fileHandle, &pointerToBuffer[offset], (uint)count, out uint numberOfBytesRead, null))
                    {
                        return (int)numberOfBytesRead;
                    }
                    else
                    {
                        throw new IOException("ReadFile failed : " + ApiUtils.LastError());
                    }
                }
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (!SetFilePointerEx(fileHandle, offset, out long position, origin))
            {
                return position;
            }
            else
            {
                throw new IOException("SetFilePointerEx : " + ApiUtils.LastError());
            }
        }

        public override void SetLength(long value)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            Position = value;
            if (!SetEndOfFile(fileHandle))
            {
                throw new IOException("SetEndOfFile failed : " + ApiUtils.LastError());
            }
        }

        public override unsafe void Write(byte[] buffer, int offset, int count)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
            else if (!canWrite)
            {
                throw new NotSupportedException("The file was not opened to write.");
            }
            else if (buffer == null)
            {
                throw new ArgumentNullException("buffer is null.");
            }
            else if (offset + count > buffer.Length)
            {
                throw new ArgumentException("The sum of offset and count is greater than the buffer length.");
            }
            else if (offset < 0 || count < 0)
            {
                throw new ArgumentOutOfRangeException("offset or count is negative.");
            }
            else
            {
                fixed (byte *pointerToBuffer = buffer)
                {
                    if (!WriteFile(fileHandle, &pointerToBuffer[offset], (uint)count, out uint numberOfBytesWritten, null))
                    {
                        throw new IOException("WriteFile failed : " + ApiUtils.LastError());
                    }
                }
            }
        }

        #region IDisposable Support
        private bool disposed = false;

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    fileHandle.Dispose();
                }

                disposed = true;
            }
        }
        #endregion
    }
}
