namespace FastLZMA2Net
{
    public partial class Decompressor : IDisposable
    {
        private readonly nint _DContext;
        private bool disposedValue;

        public uint ThreadCount => NativeMethods.FL2_getDCtxThreadCount(_DContext);

        public Decompressor(uint nbThreads = 0)
        {
            if (nbThreads == 1)
            {
                _DContext = NativeMethods.FL2_createDCtx();
            }
            else
            {
                _DContext = NativeMethods.FL2_createDCtxMt(nbThreads);
            }
        }

        public void Init(byte prop)
        {
            NativeMethods.FL2_initDCtx(_DContext, prop);
        }

        public byte[] Decompress(byte[] data)
        {
            nuint decompressedSize = FL2.FindDecompressedSize(data);
            byte[] decompressed = new byte[decompressedSize];
            nuint code = NativeMethods.FL2_decompressDCtx(_DContext, decompressed, decompressedSize, data, (nuint)data.Length);
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
                NativeMethods.FL2_freeDCtx(_DContext);
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