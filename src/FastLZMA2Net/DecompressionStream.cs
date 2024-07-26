using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace FastLZMA2Net
{
    public class DecompressionStream : Stream, IDisposable
    {
        private const int bufferSize = 81920;
        private byte[] compressedBuffer;
        GCHandle inHandle;
        FL2InBuffer inBuffer;
        private bool disposedValue = false;
        private readonly Stream _innerStream;
        private readonly nint _context;
        private readonly MemoryStream _bufferStream = new MemoryStream();
        public override bool CanRead => _innerStream != null && _innerStream.CanRead;
        public override bool CanWrite => _innerStream != null && _innerStream.CanWrite;
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public DecompressionStream(Stream innerStream)
        {
            _innerStream  = innerStream;
            _context = ExternMethods.FL2_createDStream();
            var code= ExternMethods.FL2_initDStream(_context);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            //输入的压缩buffer
            compressedBuffer = new byte[_innerStream.Length < bufferSize ? _innerStream.Length : bufferSize];
            int bytesRead= _innerStream.Read(compressedBuffer,0, compressedBuffer.Length);
            inHandle = GCHandle.Alloc(compressedBuffer, GCHandleType.Pinned);
            inBuffer = new FL2InBuffer()
            {
                src = inHandle.AddrOfPinnedObject(),
                size =(nuint) bytesRead,
                pos = 0
            };
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            //输出的解压buffer
            GCHandle outHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            FL2OutBuffer outBuffer = new FL2OutBuffer()
            {
                dst = outHandle.AddrOfPinnedObject() + offset,
                size = (nuint)count,
                pos = 0
            };
            nuint code;
            do
            {
                code = ExternMethods.FL2_decompressStream(_context, ref outBuffer, ref inBuffer);
                if (FL2Exception.IsError(code))
                {
                    if (FL2Exception.GetErrorCode(code) != FL2ErrorCode.buffer)
                    {
                        throw new FL2Exception(code);
                    }
                }
                if (inBuffer.size == inBuffer.pos)
                {
                    int bytesRead = _innerStream.Read(compressedBuffer, 0, compressedBuffer.Length);
                    inBuffer.size =(nuint) bytesRead;
                    inBuffer.pos = 0;
                }
            } while (code == 1);
            
            outHandle.Free();

            return (int)outBuffer.pos;
        }

       

        public override long Seek(long offset, SeekOrigin origin)=>throw new NotSupportedException();

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing) 
                {
                    _innerStream.Close();
                    inHandle.Free();
                }
                ExternMethods.FL2_freeDStream(_context);
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