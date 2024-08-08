using System.IO.MemoryMappedFiles;

namespace FastLZMA2Net
{
    //FL2CompressContext
    public partial class Compressor : IDisposable
    {
        private readonly nint _CContext;
        private bool disposed = false;
        private bool disposedValue;

        public uint ThreadCount => NativeMethods.FL2_getCCtxThreadCount(_CContext);
        public byte DictSizeProperty => NativeMethods.FL2_getCCtxDictProp(_CContext);

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

        public Compressor(uint nbThreads = 0, int compressLevel = 6)
        {
            if (nbThreads == 1)
            {
                _CContext = NativeMethods.FL2_createCCtx();
            }
            else
            {
                _CContext = NativeMethods.FL2_createCCtxMt(nbThreads);
            }
            CompressLevel = (int)compressLevel;
        }

        public Task<byte[]> CompressAsync(byte[] src)
        {
            return CompressAsync(src, 0);
        }

        public Task<byte[]> CompressAsync(byte[] src, int compressLevel)
        {
            return Task.Run(() => Compress(src, compressLevel));
        }

        public byte[] Compress(byte[] src)
        {
            return Compress(src, 0);
        }

        public byte[] Compress(byte[] src, int compressLevel)
        {
            if (src is null)
            {
                throw new ArgumentNullException(nameof(src));
            }

            byte[] buffer = new byte[FL2.FindCompressBound(src)];
            nuint code = NativeMethods.FL2_compressCCtx(_CContext, buffer, (nuint)buffer.Length, src, (nuint)src.Length, compressLevel);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return buffer[..(int)code];
        }

        public unsafe nuint Compress(string srcPath, string dstPath)
        {
            nuint code;
            FileInfo sourceFile = new FileInfo(srcPath);
            FileInfo destFile = new FileInfo(dstPath);
            if (sourceFile.Length >= 0x7FFFFFFF)
            {
                throw new ArgumentOutOfRangeException(nameof(srcPath), "File is too large");
            }
            if (destFile.Exists)
            {
                destFile.Delete();
            }
            using (DirectFileAccessor accessorSrc = new DirectFileAccessor(sourceFile.FullName, FileMode.Open, null, sourceFile.Length, MemoryMappedFileAccess.ReadWrite))
            {
                code = NativeMethods.FL2_compressBound((nuint)sourceFile.Length);
                if (FL2Exception.IsError(code))
                {
                    throw new FL2Exception(code);
                }
                using (DirectFileAccessor accessorDst = new DirectFileAccessor(destFile.FullName, FileMode.OpenOrCreate, null, sourceFile.Length, MemoryMappedFileAccess.ReadWrite))
                {
                    code = NativeMethods.FL2_compressCCtx(_CContext, accessorDst.mmPtr, code, accessorSrc.mmPtr, (nuint)sourceFile.Length, CompressLevel);
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

        public nuint SetParameter(FL2Parameter param, nuint value)
        {
            nuint code = NativeMethods.FL2_CCtx_setParameter(_CContext, param, value);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return code;
        }

        public nuint GetParameter(FL2Parameter param)
        {
            var code = NativeMethods.FL2_CCtx_getParameter(_CContext, param);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return code;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing) { }
                NativeMethods.FL2_freeCCtx(_CContext);
                disposedValue = true;
            }
        }

        ~Compressor()
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