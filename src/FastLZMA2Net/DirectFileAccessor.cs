using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace FastLZMA2Net
{
    public unsafe class DirectFileAccessor:IDisposable
    {
        private readonly string _filePath;
        private readonly MemoryMappedFile _mmFile;
        private MemoryMappedViewAccessor _accessor;
        public byte* mmPtr;
        private bool disposed;
        public DirectFileAccessor(string path, FileMode mode, string? mapName, long capacity,
                                                        MemoryMappedFileAccess access)
        {
            _filePath = path;
            _mmFile = MemoryMappedFile.CreateFromFile(_filePath, mode,null,capacity,access);
            _accessor = _mmFile.CreateViewAccessor();
            _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref mmPtr);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                if (disposing)
                {
                    _accessor.Dispose();
                    _mmFile.Dispose();
                    // TODO: 释放托管状态(托管对象)
                }
                // TODO: 释放未托管的资源(未托管的对象)并重写终结器

                disposed = true;
            }
        }

        ~DirectFileAccessor()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
