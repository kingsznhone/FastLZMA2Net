using System.Runtime.InteropServices;

namespace FastLZMA2Net
{
    /// <summary>
    /// Streaming Fast LZMA2 compress
    /// </summary>
    public class CompressStream : Stream
    {
        private readonly nint bufferSize;
        private byte[] outputBufferArray;
        private FL2OutBuffer outBuffer;

        private bool disposed;
        private bool _initialized;
        private bool _ended;
        private readonly Stream _innerStream;
        private readonly nint _context;
        internal nint ContextPtr => _context;

        /// <summary>
        /// Gets whether the stream can be read.
        /// </summary>
        public override bool CanRead => false;

        /// <summary>
        /// Gets whether the stream can be written to.
        /// </summary>
        public override bool CanWrite => !disposed;

        /// <summary>
        /// Gets whether the stream supports seeking.
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// Compressed output size is not available before the stream is finalized.
        /// </summary>
        public override long Length => throw new NotSupportedException();

        /// <summary>
        /// Gets or sets the current position in the stream.
        /// </summary>
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
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
        /// Levels 1..10 Setting to 1 switches to an alternate cLevel table.
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
        /// Initialize streaming compress context
        /// </summary>
        /// <param name="dstStream">compressed data store</param>
        /// <param name="nbThreads">Number of threads to use; 0 auto-selects all cores.</param>
        /// <param name="outBufferSize">Native interop buffer size, default = 64M</param>
        /// <exception cref="FL2Exception"></exception>
        public CompressStream(Stream dstStream, uint nbThreads = 0, nint outBufferSize = 64 * 1024 * 1024)
        {
            ArgumentNullException.ThrowIfNull(dstStream);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(outBufferSize);
            ArgumentOutOfRangeException.ThrowIfGreaterThan((long)outBufferSize, Array.MaxLength);
            bufferSize = outBufferSize;
            _innerStream = dstStream;
            _context = NativeMethods.FL2_createCStreamMt(nbThreads, 1);
            if (_context == IntPtr.Zero)
                throw new FL2Exception(FL2ErrorCode.MemoryAllocation);

            // Compressed stream output buffer
            outputBufferArray = GC.AllocateArray<byte>((int)bufferSize, pinned: true);
            outBuffer = new FL2OutBuffer()
            {
                dst = Marshal.UnsafeAddrOfPinnedArrayElement(outputBufferArray, 0),
                size = (nuint)outputBufferArray.Length,
                pos = 0
            };
        }

        /// <summary>
        /// Append raw data to streaming, won't close compress stream
        /// </summary>
        /// <param name="buffer">Extra data</param>
        /// <param name="offset">Start index in buffer</param>
        /// <param name="count">How many bytes to append</param>
        public void Append(byte[] buffer, int offset, int count)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            Append(buffer.AsSpan(offset, count));
        }

        /// <summary>
        /// Append raw data to streaming, won't close compress stream
        /// </summary>
        /// <param name="buffer">Extra data</param>
        public void Append(ReadOnlySpan<byte> buffer)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            CompressCore(buffer, true);
        }

        /// <summary>
        /// Start compression and finish stream.
        /// </summary>
        /// <param name="buffer">Raw data</param>
        /// <param name="offset">Start index in buffer</param>
        /// <param name="count">How many bytes to append</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            Write(buffer.AsSpan(offset, count));
        }

        /// <summary>
        /// Start compression and finish stream.
        /// </summary>
        /// <param name="buffer">Raw data</param>
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            CompressCore(buffer, false);
        }

        /// <summary>
        /// Start compression and finish stream asynchronously.
        /// </summary>
        /// <param name="buffer">Raw data</param>
        /// <param name="offset">Start index in buffer</param>
        /// <param name="count">How many bytes to append</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            Memory<byte> bufferMemory = buffer.AsMemory(offset, count);
            await new ValueTask<int>(CompressCore(bufferMemory.Span, false, cancellationToken)).ConfigureAwait(false);
        }

        /// <summary>
        /// Start compression and finish stream asynchronously.
        /// </summary>
        /// <param name="buffer">Raw data</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await new ValueTask<int>(CompressCore(buffer.Span, false, cancellationToken)).ConfigureAwait(false);
        }

        private unsafe int CompressCore(ReadOnlySpan<byte> buffer, bool Appending, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            ref byte ref_buffer = ref MemoryMarshal.GetReference(buffer);
            fixed (byte* pBuffer = &ref_buffer)
            {
                FL2InBuffer inBuffer = new()
                {
                    src = (nint)pBuffer,
                    size = (nuint)buffer.Length,
                    pos = 0
                };
                nuint code;

                //push source data & receive part of compressed data
                do
                {
                    outBuffer.pos = 0;
                    //code 1 output is full, 0 working
                    code = NativeMethods.FL2_compressStream(_context, ref outBuffer, ref inBuffer);
                    if (FL2Exception.IsError(code))
                    {
                        if (FL2Exception.GetErrorCode(code) != FL2ErrorCode.Buffer)
                        {
                            throw new FL2Exception(code);
                        }
                    }
                    _innerStream.Write(outputBufferArray, 0, (int)outBuffer.pos);
                } while (!cancellationToken.IsCancellationRequested && (outBuffer.pos != 0));
                if (cancellationToken.IsCancellationRequested)
                {
                    NativeMethods.FL2_cancelCStream(_context);
                    return 0;
                }

                // continue receive compressed data
                do
                {
                    outBuffer.pos = 0;
                    //Returns 1 if input or output still exists in the CStream object, 0 if complete,
                    code = NativeMethods.FL2_copyCStreamOutput(_context, ref outBuffer);
                    if (FL2Exception.IsError(code))
                    {
                        if (FL2Exception.GetErrorCode(code) != FL2ErrorCode.Buffer)
                        {
                            throw new FL2Exception(code);
                        }
                    }
                    _innerStream.Write(outputBufferArray, 0, (int)outBuffer.pos);
                } while (!cancellationToken.IsCancellationRequested && outBuffer.pos != 0);
                if (cancellationToken.IsCancellationRequested)
                {
                    NativeMethods.FL2_cancelCStream(_context);
                    return 0;
                }

                //Write compress checksum if not appending mode
                if (!Appending)
                {
                    // Flush remaining dictionary data and write stream end marker
                    do
                    {
                        outBuffer.pos = 0;
                        code = NativeMethods.FL2_endStream(_context, ref outBuffer);
                        if (FL2Exception.IsError(code))
                        {
                            if (FL2Exception.GetErrorCode(code) != FL2ErrorCode.Buffer)
                            {
                                throw new FL2Exception(code);
                            }
                        }
                        _innerStream.Write(outputBufferArray, 0, (int)outBuffer.pos);
                    } while (!cancellationToken.IsCancellationRequested && outBuffer.pos != 0);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        NativeMethods.FL2_cancelCStream(_context);
                        return 0;
                    }
                    //reset for next mission
                    _ended = true;
                    _initialized = false;
                }
            }
            return 0;
        }

        /// <summary>
        /// Not support
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <summary>
        /// Not support
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="NotSupportedException"></exception>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <summary>
        /// Not support
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <summary>
        /// Not support
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public override int Read(Span<byte> buffer) => throw new NotSupportedException();

        /// <summary>
        /// Not support
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public override int ReadByte() => throw new NotSupportedException();

        /// <summary>
        /// Not support
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    => throw new NotSupportedException();

        /// <summary>
        /// Not support
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        /// <summary>
        /// Write Checksum in the end. finish compress progress.
        /// </summary>
        /// <exception cref="FL2Exception"></exception>
        public override void Flush()
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (_ended) return;
            EnsureInitialized();
            nuint code;
            do
            {
                outBuffer.pos = 0;
                code = NativeMethods.FL2_endStream(_context, ref outBuffer);
                if (FL2Exception.IsError(code))
                {
                    if (FL2Exception.GetErrorCode(code) != FL2ErrorCode.Buffer)
                    {
                        throw new FL2Exception(code);
                    }
                }
                _innerStream.Write(outputBufferArray, 0, (int)outBuffer.pos);
            } while (outBuffer.pos != 0);
            //prepare for next mission
            _ended = true;
            _initialized = false;
        }

        /// <summary>
        /// Asynchronously writes the checksum and finalizes the compress stream.
        /// </summary>
        /// <exception cref="FL2Exception"></exception>
        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (_ended) return;
            EnsureInitialized();
            nuint code;
            do
            {
                outBuffer.pos = 0;
                code = NativeMethods.FL2_endStream(_context, ref outBuffer);
                if (FL2Exception.IsError(code))
                {
                    if (FL2Exception.GetErrorCode(code) != FL2ErrorCode.Buffer)
                    {
                        throw new FL2Exception(code);
                    }
                }
                await _innerStream.WriteAsync(outputBufferArray.AsMemory(0, (int)outBuffer.pos), cancellationToken).ConfigureAwait(false);
            } while (outBuffer.pos != 0);
            //prepare for next mission
            _ended = true;
            _initialized = false;
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                var code = NativeMethods.FL2_initCStream(_context, 0);
                if (FL2Exception.IsError(code))
                {
                    throw new FL2Exception(code);
                }
                _initialized = true;
                _ended = false;
            }
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
            nuint code = NativeMethods.FL2_CStream_setParameter(_context, param, value);
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
            var code = NativeMethods.FL2_CStream_getParameter(_context, param);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return code;
        }

        /// <summary>
        /// Releases the compression stream resources.
        /// </summary>
        /// <param name="disposing">True when managed resources are also being released.</param>
        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Flush();
                    _innerStream.Dispose();
                }
                if (_context != IntPtr.Zero)
                    NativeMethods.FL2_freeCStream(_context);
                disposed = true;
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Asynchronously releases managed and native resources.
        /// </summary>
        public override async ValueTask DisposeAsync()
        {
            if (!disposed)
            {
                try
                {
                    await FlushAsync(CancellationToken.None).ConfigureAwait(false);
                }
                finally
                {
                    if (_context != IntPtr.Zero)
                        NativeMethods.FL2_freeCStream(_context);
                    await _innerStream.DisposeAsync().ConfigureAwait(false);
                    disposed = true;
                }
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizes the compression stream if it was not disposed.
        /// </summary>
        ~CompressStream()
        {
            Dispose(disposing: false);
        }
    }
}