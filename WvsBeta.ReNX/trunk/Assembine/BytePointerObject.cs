// Assembine is copyright angelsl, 2011 to 2013 inclusive.
//
// This file (BytePointerObject.cs) is part of Assembine.
//
// Assembine is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Assembine is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Assembine. If not, see <http://www.gnu.org/licenses/>.
//
// Linking Assembine statically or dynamically with other modules
// is making a combined work based on Assembine. Thus, the terms and
// conditions of the GNU General Public License cover the whole combination.
//
// As a special exception, the copyright holders of Assembine give you
// permission to link Assembine with independent modules to produce an
// executable, regardless of the license terms of these independent modules,
// and to copy and distribute the resulting executable under terms of your
// choice, provided that you also meet, for each linked independent module,
// the terms and conditions of the license of that module. An independent
// module is a module which is not derived from or based on Assembine.

using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Assembine
{
    internal unsafe interface BytePointerObject : IDisposable
    {
        byte* Pointer { get; }
    }

    internal unsafe class ByteArrayPointer : BytePointerObject
    {
        private readonly byte* _start;
        private byte[] _array;
        private bool _disposed;
        private GCHandle _gcH;

        internal ByteArrayPointer(byte[] array)
        {
            _array = array;
            _gcH = GCHandle.Alloc(_array, GCHandleType.Pinned);
            _start = (byte*)_gcH.AddrOfPinnedObject();
        }

        #region BytePointerObject Members

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            if (_disposed) throw new ObjectDisposedException("Memory mapped file");
            _disposed = true;
            _gcH.Free();
            _array = null;
        }

        public byte* Pointer
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException("Memory mapped file");
                return _start;
            }
        }

        #endregion
    }

    internal unsafe class MemoryMappedFile : BytePointerObject
    {
        private static readonly string _monoPosix =
            "Mono.Posix, Version=2.0.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756";

        private static readonly bool _isLinux;
        private readonly IntPtr _fmap;
        private readonly ulong _fsize;
        private readonly IntPtr _fview;
        private readonly int _lfd;
        private readonly SafeFileHandle _sfh;
        private readonly byte* _start;
        private bool _disposed;

        static MemoryMappedFile()
        {
            var p = (int)Environment.OSVersion.Platform;
            _isLinux = p == 4 || p == 6 || p == 128;
            if (_isLinux) Assembly.Load(_monoPosix);
        }

        internal MemoryMappedFile(string path)
        {
            if (_isLinux)
            {
                _fsize = (ulong)new FileInfo(path).Length;
                _lfd = PLOpenReadonly(path);
                _fmap = PLMMap(_lfd, _fsize);
                _start = (byte*)_fmap.ToPointer();
            }
            else
            {
                _sfh = PWCreateFile(path, FileAccess.Read, FileShare.Read, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);
                if (_sfh.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
                _fmap = PWCreateFileMapping(_sfh, IntPtr.Zero, 2U, 0, 0, null);
                if (_fmap == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
                _fview = PWMapViewOfFile(_fmap, 4U, 0, 0, 0);
                if (_fview == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
                _start = (byte*)_fview.ToPointer();
            }
        }

        #region BytePointerObject Members

        public byte* Pointer
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException("Memory mapped file");
                return _start;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            if (_disposed) throw new ObjectDisposedException("Memory mapped file");
            _disposed = true;
            if (_isLinux)
            {
                PLMUnmap(_fmap, _fsize);
                PLClose(_lfd);
            }
            else
            {
                if (!PWUnmapViewOfFile(_fview)) throw new Win32Exception(Marshal.GetLastWin32Error());
                if (!PWCloseHandle(_fmap)) throw new Win32Exception(Marshal.GetLastWin32Error());
                _sfh.Dispose();
            }
        }

        #endregion

        #region mmap P/Invoke (Windows)

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "CreateFile")]
        private static extern SafeFileHandle PWCreateFile(string lpFileName,
[MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess,
[MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
                                                          IntPtr lpSecurityAttributes,
[MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
[MarshalAs(UnmanagedType.U4)] FileAttributes
                                                              dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "CreateFileMapping")]
        private static extern IntPtr PWCreateFileMapping(SafeFileHandle hFile, IntPtr lpFileMappingAttributes,
                                                         uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow,
[MarshalAs(UnmanagedType.LPTStr)] string lpName);

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "MapViewOfFile")]
        private static extern IntPtr PWMapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess,
                                                     uint dwFileOffsetHigh, uint dwFileOffsetLow,
                                                     uint dwNumberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "CloseHandle")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PWCloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "UnmapViewOfFile")]
        private static extern bool PWUnmapViewOfFile(IntPtr lpBaseAddress);

        #endregion

        #region mmap P/Invoke (Linux/Mono)

        private static int PLOpenReadonly(string path)
        {
            Type openFlags = Type.GetType(Assembly.CreateQualifiedName(_monoPosix, "Mono.Unix.Native.OpenFlags"));
            MethodInfo mi =
                Type.GetType(Assembly.CreateQualifiedName(_monoPosix, "Mono.Unix.Native.Syscall"))
                    .GetMethod("open", new[] { typeof(string), openFlags });
            return (int)mi.Invoke(null, new[] { path, openFlags.GetField("O_RDONLY").GetValue(null) });
        }

        private static int PLClose(int fd)
        {
            MethodInfo mi =
                Type.GetType(Assembly.CreateQualifiedName(_monoPosix, "Mono.Unix.Native.Syscall"))
                    .GetMethod("close", new[] { typeof(int) });
            return (int)mi.Invoke(null, new object[] { fd });
        }

        private static IntPtr PLMMap(int fd, ulong fsize)
        {
            Type mmapProts = Type.GetType(Assembly.CreateQualifiedName(_monoPosix, "Mono.Unix.Native.MmapProts"));
            Type mmapFlags = Type.GetType(Assembly.CreateQualifiedName(_monoPosix, "Mono.Unix.Native.MmapFlags"));
            MethodInfo mi =
                Type.GetType(Assembly.CreateQualifiedName(_monoPosix, "Mono.Unix.Native.Syscall"))
                    .GetMethod("mmap",
                               new[] { typeof(IntPtr), typeof(ulong), mmapProts, mmapFlags, typeof(int), typeof(long) });
            return
                (IntPtr)
                mi.Invoke(null,
                          new[] {
                                    IntPtr.Zero, fsize, mmapProts.GetField("PROT_READ").GetValue(null),
                                    mmapFlags.GetField("MAP_SHARED").GetValue(null), fd, 0
                                });
        }

        private static int PLMUnmap(IntPtr mmap, ulong fsize)
        {
            MethodInfo mi =
                Type.GetType(Assembly.CreateQualifiedName(_monoPosix, "Mono.Unix.Native.Syscall"))
                    .GetMethod("munmap", new[] { typeof(IntPtr), typeof(ulong) });
            return (int)mi.Invoke(null, new object[] { mmap, fsize });
        }

        #endregion
    }
}