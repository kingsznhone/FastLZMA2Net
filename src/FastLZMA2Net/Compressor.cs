using System.Buffers;
using System.IO.MemoryMappedFiles;

namespace FastLZMA2Net
{
    /// <summary>
    /// Fast LZMA2 Compress Context.
    /// <para>This type is not thread-safe. Do not share instances across threads without external synchronization.</para>
    /// </summary>
    public partial class Compressor : IDisposable
    {
        private readonly nint _context;
        internal nint ContextPtr => _context;

        private bool disposed;

        /// <summary>
        /// Gets the number of threads used by the compression context.
        /// </summary>
        public uint ThreadCount
        {
            get
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                return NativeMethods.FL2_getCCtxThreadCount(_context);
            }
        }

        /// <summary>
        /// Gets the encoded dictionary size property byte stored in the compressed stream header.
        /// </summary>
        public byte DictSizeProperty
        {
            get
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                return NativeMethods.FL2_getCCtxDictProp(_context);
            }
        }

        /// <summary>
        /// Compress Level [1..10]
        /// </summary>
        public int CompressLevel
        {
            get => (int)GetParameter(FL2Parameter.CompressionLevel);
            set => SetParameter(FL2Parameter.CompressionLevel, (nuint)value);
        }

        /// <summary>
        /// Maximizes compression ratio for a given dictionary size. Levels 1..10 map dictionaryLog 20..29 (1 MB..512 MB).
        /// Setting to 0 disables high-compression mode.
        /// </summary>
        public int HighCompressLevel
        {
            get => (int)GetParameter(FL2Parameter.HighCompression);
            set => SetParameter(FL2Parameter.HighCompression, (nuint)value);
        }

        /// <summary>
        /// Dictionary size with FL2.DictSizeMin and FL2.DictSizeMax
        /// </summary>
        public int DictionarySize
        {
            get => (int)GetParameter(FL2Parameter.DictionarySize);
            set => SetParameter(FL2Parameter.DictionarySize, (nuint)value);
        }

        /// <summary>
        /// Match finder will resolve string matches up to this length.
        /// If a longer match exists further back in the input, it will not be found.
        /// Default = 42
        /// </summary>
        public int SearchDepth
        {
            get => (int)GetParameter(FL2Parameter.SearchDepth);
            set => SetParameter(FL2Parameter.SearchDepth, (nuint)value);
        }

        /// <summary>
        /// Only useful for strategies >= opt.
        /// Length of match considered "good enough" to stop search.
        /// Larger values make compression stronger and slower.
        /// Default = 48
        /// </summary>
        public int FastLength
        {
            get => (int)GetParameter(FL2Parameter.FastLength);
            set => SetParameter(FL2Parameter.FastLength, (nuint)value);
        }

        /// <summary>
        /// Initialize new compress context
        /// </summary>
        /// <param name="nbThreads">Number of threads to use; 0 auto-selects all cores.</param>
        /// <param name="compressLevel">Initial compression level (1–10, default 6).</param>
        public Compressor(uint nbThreads = 0, int compressLevel = 6)
        {
            _context = NativeMethods.FL2_createCCtxMt(nbThreads);
            if (_context == IntPtr.Zero)
                throw new FL2Exception(FL2ErrorCode.MemoryAllocation);
            CompressLevel = compressLevel;
        }

        /// <summary>
        /// Compresses data asynchronously using the current compression level.
        /// This is CPU-bound work dispatched to the thread pool; cancellation prevents the task from starting.
        /// </summary>
        /// <param name="src">Source data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The compressed byte array.</returns>
        public Task<byte[]> CompressAsync(byte[] src, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return CompressAsync(src, CompressLevel, cancellationToken);
        }

        /// <summary>
        /// Compresses data asynchronously with the specified compression level.
        /// This is CPU-bound work dispatched to the thread pool; cancellation prevents the task from starting.
        /// </summary>
        /// <param name="src">Source data.</param>
        /// <param name="compressLevel">Compression level to use.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The compressed byte array.</returns>
        public Task<byte[]> CompressAsync(byte[] src, int compressLevel, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return Task.Run(() => Compress(src, compressLevel), cancellationToken);
        }

        /// <summary>
        /// Compresses data asynchronously from a <see cref="ReadOnlyMemory{T}"/> source.
        /// This is CPU-bound work dispatched to the thread pool; cancellation prevents the task from starting.
        /// </summary>
        /// <param name="src">Source data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The compressed byte array.</returns>
        public Task<byte[]> CompressAsync(ReadOnlyMemory<byte> src, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return Task.Run(() => Compress(src.Span), cancellationToken);
        }

        /// <summary>
        /// Compresses data using the current compression level.
        /// </summary>
        /// <param name="src">Source data.</param>
        /// <returns>The compressed byte array.</returns>
        public byte[] Compress(byte[] src)
        {
            return Compress(src, 0);
        }

        /// <summary>
        /// Compress data with compress level setting
        /// </summary>
        /// <param name="src">Data byte array</param>
        /// <param name="compressLevel">compress level</param>
        /// <returns>Bytes Compressed</returns>
        /// <exception cref="FL2Exception">Thrown when compression fails.</exception>
        public byte[] Compress(byte[] src, int compressLevel)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            ArgumentNullException.ThrowIfNull(src);

            nuint bound = FL2.FindCompressBound(src);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(checked((int)bound));
            try
            {
                nuint code = NativeMethods.FL2_compressCCtx(_context, buffer, bound, src, (nuint)src.Length,
                    compressLevel == 0 ? CompressLevel : compressLevel);
                if (FL2Exception.IsError(code))
                {
                    throw new FL2Exception(code);
                }
                return buffer.AsSpan(0, checked((int)code)).ToArray();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Compresses data from a <see cref="ReadOnlySpan{T}"/> source. Avoids a copy when the caller
        /// already holds data in a pooled or stack-allocated buffer.
        /// </summary>
        /// <param name="src">Source data.</param>
        /// <param name="compressLevel">Compression level to use, or 0 to use the current setting.</param>
        /// <returns>The compressed byte array.</returns>
        public byte[] Compress(ReadOnlySpan<byte> src, int compressLevel = 0)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            nuint bound = FL2.FindCompressBound((nuint)src.Length);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(checked((int)bound));
            try
            {
                nuint code = NativeMethods.FL2_compressCCtx(_context, buffer, bound, src, (nuint)src.Length,
                    compressLevel == 0 ? CompressLevel : compressLevel);
                if (FL2Exception.IsError(code))
                    throw new FL2Exception(code);
                return buffer.AsSpan(0, checked((int)code)).ToArray();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Compress file using direct file access. No memory copy overhead.
        /// </summary>
        /// <param name="srcPath">source file path</param>
        /// <param name="dstPath">output file path</param>
        /// <returns>Bytes Compressed</returns>
        /// <exception cref="FL2Exception"></exception>
        public nuint Compress(string srcPath, string dstPath)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            ArgumentException.ThrowIfNullOrWhiteSpace(srcPath);
            ArgumentException.ThrowIfNullOrWhiteSpace(dstPath);
            nuint code;
            FileInfo sourceFile = new(srcPath);
            FileInfo destFile = new(dstPath);
            if (destFile.Exists)
            {
                destFile.Delete();
            }
            using (DirectFileAccessor accessorSrc = new(sourceFile.FullName, FileMode.Open, null, sourceFile.Length, MemoryMappedFileAccess.Read))
            {
                code = NativeMethods.FL2_compressBound((nuint)sourceFile.Length);
                if (FL2Exception.IsError(code))
                {
                    throw new FL2Exception(code);
                }
                using (DirectFileAccessor accessorDst = new(destFile.FullName, FileMode.OpenOrCreate, null, (long)code, MemoryMappedFileAccess.ReadWrite))
                {
                    code = NativeMethods.FL2_compressCCtx(_context, accessorDst.AsSpan(), code, accessorSrc.AsReadOnlySpan(), (nuint)sourceFile.Length, CompressLevel);
                    if (FL2Exception.IsError(code))
                    {
                        throw new FL2Exception(code);
                    }
                }
            }

            using (var tmp = File.OpenWrite(destFile.FullName))
            {
                tmp.SetLength((long)code);
            }
            return code;
        }

        /// <summary>
        /// Set detail compress parameter
        /// </summary>
        /// <param name="param"> Parameter Enum</param>
        /// <param name="value">The value to assign to the parameter.</param>
        /// <returns>The raw native return value (0 on success); throws <see cref="FL2Exception"/> on error.</returns>
        /// <exception cref="FL2Exception"></exception>
        public nuint SetParameter(FL2Parameter param, nuint value)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            nuint code = NativeMethods.FL2_CCtx_setParameter(_context, param, value);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return code;
        }

        /// <summary>
        /// Get detail compress parameter
        /// </summary>
        /// <param name="param"> Parameter Enum</param>
        /// <returns>Parameter Value</returns>
        /// <exception cref="FL2Exception"></exception>
        public nuint GetParameter(FL2Parameter param)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            var code = NativeMethods.FL2_CCtx_getParameter(_context, param);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return code;
        }

        /// <summary>
        /// Releases the unmanaged compression context.
        /// </summary>
        /// <param name="disposing">True when managed resources are also being released.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing) { }
                NativeMethods.FL2_freeCCtx(_context);
                disposed = true;
            }
        }

        /// <summary>
        /// Finalizes the compressor if <see cref="Dispose()" /> was not called.
        /// </summary>
        ~Compressor()
        {
            Dispose(disposing: false);
        }

        /// <summary>
        /// Releases the compression context and suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}