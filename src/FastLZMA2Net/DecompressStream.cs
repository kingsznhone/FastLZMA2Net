using System.Runtime.InteropServices;

namespace FastLZMA2Net
{
    /// <summary>
    /// Streaming decompress context
    /// </summary>
    public class DecompressStream : Stream
    {
        private readonly nint bufferSize = 16 * 1024 * 1024;
        private byte[] inputBufferArray;
        private FL2InBuffer inBuffer;
        private bool disposed;
        private readonly Stream _innerStream;
        private readonly nint _context;
        internal nint ContextPtr => _context;

        /// <summary>
        /// Gets whether the stream can be read.
        /// </summary>
        public override bool CanRead => !disposed;

        /// <summary>
        /// Gets whether the stream can be written to.
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        /// Gets whether the stream supports seeking.
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// Can't determine data size
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
        /// Initialize streaming decompress context
        /// </summary>
        /// <param name="srcStream">compressed data store</param>
        /// <param name="nbThreads">Number of threads to use; 0 auto-selects all cores, 1 uses a single-threaded context.</param>
        /// <param name="inBufferSize">Native interop buffer size, default = 64M</param>
        /// <exception cref="FL2Exception"></exception>
        public DecompressStream(Stream srcStream, uint nbThreads = 0, nint inBufferSize = 64 * 1024 * 1024)
        {
            ArgumentNullException.ThrowIfNull(srcStream);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(inBufferSize);
            ArgumentOutOfRangeException.ThrowIfGreaterThan((long)inBufferSize, Array.MaxLength);
            bufferSize = inBufferSize;
            _innerStream = srcStream;

            if (nbThreads == 1)
            {
                _context = NativeMethods.FL2_createDStream();
            }
            else
            {
                _context = NativeMethods.FL2_createDStreamMt(nbThreads);
            }
            if (_context == IntPtr.Zero)
                throw new FL2Exception(FL2ErrorCode.MemoryAllocation);
            var code = NativeMethods.FL2_initDStream(_context);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }

            // Use the stream's known length only when it is seekable; fall back to bufferSize for
            // non-seekable sources (NetworkStream, GZipStream, etc.) where Length throws.
            nint safeLength = _innerStream.CanSeek
                ? (nint)Math.Min(_innerStream.Length, (long)bufferSize)
                : bufferSize;
            inputBufferArray = GC.AllocateArray<byte>((int)safeLength, pinned: true);
            int bytesRead = _innerStream.Read(inputBufferArray, 0, inputBufferArray.Length);
            inBuffer = new FL2InBuffer()
            {
                src = Marshal.UnsafeAddrOfPinnedArrayElement(inputBufferArray, 0),
                size = (nuint)bytesRead,
                pos = 0
            };
        }

        /// <summary>
        /// Copy decompressed data to destination stream
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="bufferSize">Default = 256M</param>
        public override void CopyTo(Stream destination, int bufferSize = 256 * 1024 * 1024)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            ArgumentNullException.ThrowIfNull(destination);
            byte[] outBufferArray = new byte[bufferSize];
            Span<byte> outBufferSpan = outBufferArray.AsSpan();
            int bytesRead;
            do
            {
                bytesRead = Read(outBufferSpan);
                destination.Write(outBufferArray, 0, bytesRead);
            } while (bytesRead != 0);
        }

        /// <summary>
        /// Gets the number of bytes decompressed so far.
        /// </summary>
        public ulong Progress
        {
            get
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                return NativeMethods.FL2_getDStreamProgress(_context);
            }
        }

        /// <summary>
        /// Read decompressed data
        /// </summary>
        /// <param name="buffer">buffer array to receive data</param>
        /// <param name="offset">Start index in buffer</param>
        /// <param name="count">How many bytes to append</param>
        /// <returns>How many bytes read.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return Read(buffer.AsSpan(offset, count));
        }

        /// <summary>
        /// Read decompressed data
        /// </summary>
        /// <param name="buffer">buffer array to receive data</param>
        /// <returns>How many bytes read.</returns>
        public override int Read(Span<byte> buffer)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return DecompressCore(buffer);
        }

        /// <summary>
        /// Reads decompressed data asynchronously.
        /// </summary>
        /// <param name="buffer">buffer array to receive data</param>
        /// <param name="offset">Start index in buffer</param>
        /// <param name="count">How many bytes to append</param>
        /// <param name="cancellationToken"></param>
        /// <returns>How many bytes read.</returns>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            Memory<byte> bufferMemory = buffer.AsMemory(offset, count);
            return ReadAsync(bufferMemory, cancellationToken).AsTask();
        }

        /// <summary>
        /// Reads decompressed data asynchronously.
        /// </summary>
        /// <param name="buffer">buffer array to receive data</param>
        /// <param name="cancellationToken"></param>
        /// <returns>How many bytes read.</returns>
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return new ValueTask<int>(DecompressCore(buffer.Span, cancellationToken));
        }

        private unsafe int DecompressCore(Span<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Set the memory limit for the decompression stream under MT. Otherwise decode will failed if buffer is too small.
            // Guess 64mb buffer is enough for most case.
            //NativeMethods.FL2_setDStreamMemoryLimitMt(_context, (nuint)64 * 1024 * 1024);
            ref byte ref_buffer = ref MemoryMarshal.GetReference(buffer);
            fixed (byte* pBuffer = &ref_buffer)
            {
                FL2OutBuffer outBuffer = new()
                {
                    dst = (nint)pBuffer,
                    size = (nuint)buffer.Length,
                    pos = 0
                };

                nuint code;
                do
                {
                    // 0 finish,1 decoding
                    code = NativeMethods.FL2_decompressStream(_context, ref outBuffer, ref inBuffer);
                    if (FL2Exception.IsError(code))
                    {
                        if (FL2Exception.GetErrorCode(code) != FL2ErrorCode.Buffer)
                        {
                            throw new FL2Exception(code);
                        }
                    }
                    //output is full
                    if (outBuffer.pos == outBuffer.size)
                    {
                        break;
                    }
                    //decode complete and no more input
                    if (code == 0 && inBuffer.size == 0)
                    {
                        break;
                    }
                    if (code == 0 || inBuffer.size == inBuffer.pos)
                    {
                        int bytesRead = _innerStream.Read(inputBufferArray, 0, inputBufferArray.Length);
                        inBuffer.size = (nuint)bytesRead;
                        inBuffer.pos = 0;
                    }
                } while (!cancellationToken.IsCancellationRequested);
                return (int)outBuffer.pos;
            }
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
        /// <exception cref="NotSupportedException"></exception>
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <summary>
        /// Not support
        /// </summary>
        /// <param name="buffer"></param>
        /// <exception cref="NotSupportedException"></exception>
        public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException();

        /// <summary>
        /// Not support
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        /// <summary>
        /// Not support
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        /// <summary>
        /// Not support
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="NotSupportedException"></exception>
        public override void WriteByte(byte value)
            => throw new NotSupportedException();

        /// <summary>
        /// No-op for a read-only decompression stream.
        /// </summary>
        public override void Flush()
        {
            ObjectDisposedException.ThrowIf(disposed, this);
        }

        /// <summary>
        /// No-op for a read-only decompression stream.
        /// </summary>
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Releases the decompression stream resources.
        /// </summary>
        /// <param name="disposing">True when managed resources are also being released.</param>
        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    _innerStream?.Dispose();
                }
                if (_context != IntPtr.Zero)
                    NativeMethods.FL2_freeDStream(_context);
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
                disposed = true;
                if (_context != IntPtr.Zero)
                    NativeMethods.FL2_freeDStream(_context);
                await _innerStream.DisposeAsync().ConfigureAwait(false);
            }
            GC.SuppressFinalize(this);
            await base.DisposeAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Finalizes the decompression stream if it was not disposed.
        /// </summary>
        ~DecompressStream()
        {
            Dispose(disposing: false);
        }
    }
}