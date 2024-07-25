namespace FastLZMA2Net
{
    public partial class Decompressor : IDisposable
    {
        private const string LibraryName = "fast-lzma2";

        private nint _context;
        private bool disposedValue;

        public uint ThreadCount => ExternMethods.FL2_getDCtxThreadCount(_context);

        public Decompressor()
        {
            _context = ExternMethods.FL2_createDCtx();
        }

        public Decompressor(uint nThread)
        {
            _context = ExternMethods.FL2_createDCtxMt(nThread);
        }

        public void Init(byte prop)
        {
            ExternMethods.FL2_initDCtx(_context, prop);
        }

        public byte[] Decompress(byte[] data)
        {
            nuint decompressedSize = FL2.FindDecompressedSize(data);
            byte[] decompressed = new byte[decompressedSize];
            nuint code = ExternMethods.FL2_decompressDCtx(_context, decompressed, decompressedSize, data, (nuint)data.Length);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return decompressed;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing) { }
                ExternMethods.FL2_freeDCtx(_context);
                disposedValue = true;
            }
        }

        ~Decompressor()
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