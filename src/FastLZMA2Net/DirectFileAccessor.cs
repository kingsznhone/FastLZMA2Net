using System.IO.MemoryMappedFiles;

namespace FastLZMA2Net
{
    internal unsafe class DirectFileAccessor : IDisposable
    {
        private readonly MemoryMappedFile _mmFile;
        private readonly MemoryMappedViewAccessor _accessor;
        private byte* _mmPtr;
        private bool disposed;

        public readonly long Length;
        public readonly string FullName;

        public DirectFileAccessor(string path, FileMode mode, string? mapName, long capacity, MemoryMappedFileAccess access)
        {
            var fileInfo = new FileInfo(path);
            _mmFile = MemoryMappedFile.CreateFromFile(path, mode, null, capacity, access);
            _accessor = _mmFile.CreateViewAccessor(0, 0, access);
            _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref _mmPtr);
            Length = capacity;
            FullName = fileInfo.FullName;
        }

        /// <summary>
        /// Returns a writable span over the memory-mapped region.
        /// </summary>
        public Span<byte> AsSpan() => new(_mmPtr, checked((int)Length));

        /// <summary>
        /// Returns a read-only span over the memory-mapped region.
        /// </summary>
        public ReadOnlySpan<byte> AsReadOnlySpan() => new(_mmPtr, checked((int)Length));

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