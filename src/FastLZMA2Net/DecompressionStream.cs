using System.Runtime.InteropServices;

namespace FastLZMA2Net
{
    public class DecompressionStream : Stream, IDisposable
    {
        private readonly int bufferSize = 16 * 1024 * 1024;
        private byte[] inputBufferArray;
        private GCHandle inputBufferHandle;
        private FL2InBuffer inBuffer;
        private bool disposed = false;
        private readonly Stream _innerStream;
        private readonly nint _context;
        public override bool CanRead => _innerStream != null && _innerStream.CanRead;
        public override bool CanWrite => false;
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public DecompressionStream(Stream innerStream, uint nbThreads = 0, int inBufferSize = 16 * 1024 * 1024)
        {
            bufferSize = inBufferSize;
            _innerStream = innerStream;

            if (nbThreads == 1)
            {
                _context = NativeMethods.FL2_createDStream();
            }
            else
            {
                _context = NativeMethods.FL2_createDStreamMt(nbThreads);
            }
            var code = NativeMethods.FL2_initDStream(_context);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }

            // Compressed stream input buffer
            inputBufferArray = new byte[_innerStream.Length < bufferSize ? _innerStream.Length : bufferSize];
            int bytesRead = _innerStream.Read(inputBufferArray, 0, inputBufferArray.Length);
            inputBufferHandle = GCHandle.Alloc(inputBufferArray, GCHandleType.Pinned);
            inBuffer = new FL2InBuffer()
            {
                src = inputBufferHandle.AddrOfPinnedObject(),
                size = (nuint)bytesRead,
                pos = 0
            };
        }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public new void CopyTo(Stream destination) => CopyTo(destination, 256 * 1024 * 1024);

        public override void CopyTo(Stream destination, int bufferSize = 256 * 1024 * 1024)
        {
            byte[] outBufferArray = new byte[bufferSize];
            Span<byte> outBufferSpan = outBufferArray.AsSpan();
            int bytesRead = 0;
            do
            {
                bytesRead = Read(outBufferSpan);
                destination.Write(outBufferArray, 0, bytesRead);
            } while (bytesRead != 0);
        }

        public ulong Progress => NativeMethods.FL2_getDStreamProgress(_context);

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Read(buffer.AsSpan(offset, count));
        }

        public override int Read(Span<byte> buffer)
        {
            return DecompressCore(buffer);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Memory<byte> bufferMemory = buffer.AsMemory(offset, count);
            return ReadAsync(buffer, cancellationToken).AsTask();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            _innerStream.ReadAsync(new byte[10], 0, 0);
            return new ValueTask<int>(DecompressCore(buffer.Span, cancellationToken));
        }

        private unsafe int DecompressCore(Span<byte> buffer, CancellationToken cancellationToken = default)
        {
            // Set the memory limit for the decompression stream under MT. Otherwise decode will failed if buffer is too small.
            // Guess 64mb buffer is enough for most case.
            //NativeMethods.FL2_setDStreamMemoryLimitMt(_context, (nuint)64 * 1024 * 1024);
            ref byte ref_buffer = ref MemoryMarshal.GetReference(buffer);
            fixed (byte* pBuffer = &ref_buffer)
            {
                FL2OutBuffer outBuffer = new FL2OutBuffer()
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
                        if (FL2Exception.GetErrorCode(code) != FL2ErrorCode.buffer)
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

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException();

        public override int ReadByte() => throw new NotSupportedException();

        public override void Close() => Dispose(true);

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public override void WriteByte(byte value)
            => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    inputBufferHandle.Free();
                }
                NativeMethods.FL2_freeDStream(_context);
                disposed = true;
            }
        }

        ~DecompressionStream()
        {
            Dispose(disposing: false);
        }

        public new void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}