using System.Runtime.InteropServices;

namespace FastLZMA2Net
{
    public class DecompressionStream : Stream, IDisposable
    {
        private readonly int bufferSize = 81920;
        private byte[] inputBufferArray;
        private GCHandle inputBufferHandle;
        private FL2InBuffer inBuffer;
        private bool disposedValue = false;
        private readonly Stream _innerStream;
        private readonly nint _context;
        public override bool CanRead => _innerStream != null && _innerStream.CanRead;
        public override bool CanWrite => _innerStream != null && _innerStream.CanWrite;
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public DecompressionStream(Stream innerStream) : this(innerStream, 81920)
        {
        }

        public DecompressionStream(Stream innerStream, int inBufferSize)
        {
            bufferSize = inBufferSize;
            _innerStream = innerStream;
            _context = NativeMethods.FL2_createDStream();
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

        public override void CopyTo(Stream destination, int bufferSize)
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

        public override unsafe int Read(byte[] buffer, int offset, int count)
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
                    code = NativeMethods.FL2_decompressStream(_context, ref outBuffer, ref inBuffer);
                    if (FL2Exception.IsError(code))
                    {
                        if (FL2Exception.GetErrorCode(code) != FL2ErrorCode.buffer)
                        {
                            throw new FL2Exception(code);
                        }
                    }
                    if (inBuffer.size == inBuffer.pos)
                    {
                        int bytesRead = _innerStream.Read(inputBufferArray, 0, inputBufferArray.Length);
                        inBuffer.size = (nuint)bytesRead;
                        inBuffer.pos = 0;
                    }
                } while (code == 1 || !cancellationToken.IsCancellationRequested);
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
            if (!disposedValue)
            {
                if (disposing)
                {
                    inputBufferHandle.Free();
                }
                NativeMethods.FL2_freeDStream(_context);
                disposedValue = true;
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