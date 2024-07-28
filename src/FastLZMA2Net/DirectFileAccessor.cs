using System.IO.MemoryMappedFiles;

namespace FastLZMA2Net
{
    internal unsafe class DirectFileAccessor : IDisposable
    {
        private readonly string _filePath;
        private readonly MemoryMappedFile _mmFile;
        private readonly MemoryMappedViewAccessor _accessor;

        private bool disposed;

        public byte* mmPtr;
        public readonly long Length;
        public readonly string FullName;

        public DirectFileAccessor(string path, FileMode mode, string? mapName, long capacity, MemoryMappedFileAccess access)
        {
            _filePath = path;
            var fileInfo = new FileInfo(path);
            _mmFile = MemoryMappedFile.CreateFromFile(_filePath, mode, null, capacity, access);
            _accessor = _mmFile.CreateViewAccessor();
            _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref mmPtr);
            Length = capacity;
            FullName = fileInfo.FullName;
        }

        public void Close() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                if (disposing)
                {
                    _accessor.Dispose();
                    _mmFile.Dispose();
                }

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