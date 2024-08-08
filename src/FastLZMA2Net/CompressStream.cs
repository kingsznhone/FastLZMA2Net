using System.Runtime.InteropServices;

namespace FastLZMA2Net
{
    public class CompressStream : Stream, IDisposable
    {
        private readonly int bufferSize;
        private byte[] outputBufferArray;
        private GCHandle outputBufferHandle;
        private FL2OutBuffer outBuffer;

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

        public int CompressLevel
        {
            get => (int)GetParameter(FL2Parameter.CompressionLevel);
            set => SetParameter(FL2Parameter.CompressionLevel, (nuint)value);
        }

        public int HighCompressLevel
        {
            get => (int)GetParameter(FL2Parameter.HighCompression);
            set => SetParameter(FL2Parameter.HighCompression, (nuint)value);
        }

        public int DictionarySize
        {
            get => (int)GetParameter(FL2Parameter.DictionarySize);
            set => SetParameter(FL2Parameter.DictionarySize, (nuint)value);
        }

        public int SearchDepth
        {
            get => (int)GetParameter(FL2Parameter.SearchDepth);
            set => SetParameter(FL2Parameter.SearchDepth, (nuint)value);
        }

        public int FastLength
        {
            get => (int)GetParameter(FL2Parameter.FastLength);
            set => SetParameter(FL2Parameter.FastLength, (nuint)value);
        }

        public CompressStream(Stream innerStream, uint nbThreads = 0, int outBufferSize = 64 * 1024 * 1024)
        {
            bufferSize = outBufferSize;
            _innerStream = innerStream;
            _context = NativeMethods.FL2_createCStreamMt(nbThreads, 1);
            var code = NativeMethods.FL2_initCStream(_context, 0);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }

            // Compressed stream output buffer
            outputBufferArray = new byte[bufferSize];
            outputBufferHandle = GCHandle.Alloc(outputBufferArray, GCHandleType.Pinned);
            outBuffer = new FL2OutBuffer()
            {
                dst = outputBufferHandle.AddrOfPinnedObject(),
                size = (nuint)outputBufferArray.Length,
                pos = 0
            };
        }

        public void Append(byte[] buffer, int offset, int count)
        {
            Append(buffer.AsSpan(offset, count));
        }

        public void Append(ReadOnlySpan<byte> buffer)
        {
            CompressCore(buffer, true);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Write(buffer.AsSpan(offset, count));
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            CompressCore(buffer, false);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Memory<byte> bufferMemory = buffer.AsMemory(offset, count);
            await new ValueTask<int>(CompressCore(bufferMemory.Span, false, cancellationToken)).ConfigureAwait(false);
            return;
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await new ValueTask<int>(CompressCore(buffer.Span, false, cancellationToken)).ConfigureAwait(false);
            return;
        }

        private unsafe int CompressCore(ReadOnlySpan<byte> buffer, bool Appending, CancellationToken cancellationToken = default)
        {
            ref byte ref_buffer = ref MemoryMarshal.GetReference(buffer);
            fixed (byte* pBuffer = &ref_buffer)
            {
                FL2InBuffer inBuffer = new FL2InBuffer()
                {
                    src = (nint)pBuffer,
                    size = (nuint)buffer.Length,
                    pos = 0
                };
                nuint code;

                //push source data& receive part of compressed data
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
                        if (FL2Exception.IsError(code))
                        {
                            if (FL2Exception.GetErrorCode(code) != FL2ErrorCode.Buffer)
                            {
                                throw new FL2Exception(code);
                            }
                        }
                    }
                    _innerStream.Write(outputBufferArray, 0, (int)outBuffer.pos);
                } while (!cancellationToken.IsCancellationRequested && outBuffer.pos != 0);
                if (cancellationToken.IsCancellationRequested)
                {
                    NativeMethods.FL2_cancelCStream(_context);
                    return 0;
                }

                // receive all remaining compressed data
                do
                {
                    outBuffer.pos = 0;
                    //Returns 1 if input or output still exists in the CStream object, 0 if complete,
                    code = NativeMethods.FL2_flushStream(_context, ref outBuffer);
                    if (FL2Exception.IsError(code))
                    {
                        if (FL2Exception.IsError(code))
                        {
                            if (FL2Exception.GetErrorCode(code) != FL2ErrorCode.Buffer)
                            {
                                throw new FL2Exception(code);
                            }
                        }
                    }
                    _innerStream.Write(outputBufferArray, 0, (int)outBuffer.pos);
                } while (!cancellationToken.IsCancellationRequested && outBuffer.pos != 0);
                if (cancellationToken.IsCancellationRequested)
                {
                    NativeMethods.FL2_cancelCStream(_context);
                    return 0;
                }

                //Write compress checksum
                if (!Appending)
                {
                    code = NativeMethods.FL2_endStream(_context, ref outBuffer);
                    if (FL2Exception.IsError(code))
                    {
                        if (FL2Exception.IsError(code))
                        {
                            if (FL2Exception.GetErrorCode(code) != FL2ErrorCode.Buffer)
                            {
                                throw new FL2Exception(code);
                            }
                        }
                    }
                    _innerStream.Write(outputBufferArray, 0, (int)outBuffer.pos);
                    //reset for next mission
                    code = NativeMethods.FL2_initCStream(_context, 0);
                    if (FL2Exception.IsError(code))
                    {
                        throw new FL2Exception(code);
                    }
                }
            }
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override int Read(Span<byte> buffer) => throw new NotSupportedException();

        public override int ReadByte() => throw new NotSupportedException();

        public override void Close() => Dispose(true);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public override void Flush()
        {
            var code = NativeMethods.FL2_endStream(_context, ref outBuffer);
            if (FL2Exception.IsError(code))
            {
                if (FL2Exception.GetErrorCode(code) != FL2ErrorCode.Buffer)
                {
                    throw new FL2Exception(code);
                }
            }
            _innerStream.Write(outputBufferArray, 0, (int)outBuffer.pos);
            //prepare for next mission
            code = NativeMethods.FL2_initCStream(_context, 0);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
        }

        public nuint SetParameter(FL2Parameter param, nuint value)
        {
            nuint code = NativeMethods.FL2_CStream_setParameter(_context, param, value);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return code;
        }

        public nuint GetParameter(FL2Parameter param)
        {
            var code = NativeMethods.FL2_CStream_getParameter(_context, param);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return code;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                _innerStream.Flush();
                if (disposing)
                {
                    outputBufferHandle.Free();
                }
                NativeMethods.FL2_freeCStream(_context);
                disposed = true;
            }
        }

        ~CompressStream()
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