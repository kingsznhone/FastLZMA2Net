namespace FastLZMA2Net
{
    /// <summary>
    /// Fast LZMA2 Decompress Context.
    /// <para>This type is not thread-safe. Do not share instances across threads without external synchronization.</para>
    /// </summary>
    public partial class Decompressor : IDisposable
    {
        private readonly nint _context;
        internal nint ContextPtr => _context;
        private bool disposed;

        /// <summary>
        /// Thread use of the context
        /// </summary>
        public uint ThreadCount
        {
            get
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                return NativeMethods.FL2_getDCtxThreadCount(_context);
            }
        }

        /// <summary>
        /// Initialize new decompress context
        /// </summary>
        /// <param name="nbThreads"></param>
        public Decompressor(uint nbThreads = 0)
        {
            if (nbThreads == 1)
            {
                _context = NativeMethods.FL2_createDCtx();
            }
            else
            {
                _context = NativeMethods.FL2_createDCtxMt(nbThreads);
            }
            if (_context == IntPtr.Zero)
                throw new FL2Exception(FL2ErrorCode.MemoryAllocation);
        }

        /// <summary>
        /// Initial new context with specific dict size property
        /// </summary>
        /// <param name="prop">dictSizeProperty</param>
        public void Init(byte prop)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            NativeMethods.FL2_initDCtx(_context, prop);
        }

        /// <summary>
        /// Decompress
        /// </summary>
        /// <param name="data">Fast LZMA2 data</param>
        /// <returns>Raw data</returns>
        /// <exception cref="FL2Exception"></exception>
        public byte[] Decompress(byte[] data)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            ArgumentNullException.ThrowIfNull(data);
            nuint decompressedSize = FL2.FindDecompressedSize(data);
            byte[] decompressed = new byte[checked((int)decompressedSize)];
            nuint code = NativeMethods.FL2_decompressDCtx(_context, decompressed, decompressedSize, data, (nuint)data.Length);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return code == decompressedSize ? decompressed : decompressed[0..checked((int)code)];
        }

        /// <summary>
        /// Decompresses data from a <see cref="ReadOnlySpan{T}"/> source. Avoids a copy when the caller
        /// already holds data in a pooled or stack-allocated buffer.
        /// </summary>
        public unsafe byte[] Decompress(ReadOnlySpan<byte> data)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            nuint decompressedSize;
            fixed (byte* pSrc = data)
            {
                decompressedSize = NativeMethods.FL2_findDecompressedSize(pSrc, (nuint)data.Length);
            }
            if (FL2Exception.IsError(decompressedSize))
                throw new FL2Exception(decompressedSize);
            byte[] decompressed = new byte[checked((int)decompressedSize)];
            nuint code;
            fixed (byte* pSrc = data)
            fixed (byte* pDst = decompressed)
            {
                code = NativeMethods.FL2_decompressDCtx(_context, pDst, decompressedSize, pSrc, (nuint)data.Length);
            }
            if (FL2Exception.IsError(code))
                throw new FL2Exception(code);
            return code == decompressedSize ? decompressed : decompressed[0..checked((int)code)];
        }

        /// <summary>
        /// Decompresses data asynchronously.
        /// This is CPU-bound work dispatched to the thread pool; cancellation prevents the task from starting.
        /// </summary>
        public Task<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            ArgumentNullException.ThrowIfNull(data);
            return Task.Run(() => Decompress(data), cancellationToken);
        }

        /// <summary>
        /// Decompresses data asynchronously from a <see cref="ReadOnlyMemory{T}"/> source.
        /// This is CPU-bound work dispatched to the thread pool; cancellation prevents the task from starting.
        /// </summary>
        public Task<byte[]> DecompressAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return Task.Run(() => Decompress(data.Span), cancellationToken);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing) { }
                NativeMethods.FL2_freeDCtx(_context);
                disposed = true;
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