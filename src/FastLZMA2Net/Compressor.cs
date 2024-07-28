using System.IO.MemoryMappedFiles;

namespace FastLZMA2Net
{
    //FL2CompressContext
    public partial class Compressor : IDisposable
    {
        private readonly nint _context;
        private bool disposed = false;
        private bool disposedValue;

        public uint ThreadCount => NativeMethods.FL2_getCCtxThreadCount(_context);
        public byte DictSizeProperty => NativeMethods.FL2_getCCtxDictProp(_context);

        public int CompressLevel
        {
            get => (int)GetParameter(CompressParameterEnum.FL2_p_compressionLevel);
            set => SetParameter(CompressParameterEnum.FL2_p_compressionLevel, (nuint)value);
        }

        public int HighCompressLevel
        {
            get => (int)GetParameter(CompressParameterEnum.FL2_p_highCompression);
            set => SetParameter(CompressParameterEnum.FL2_p_highCompression, (nuint)value);
        }

        public int DictionarySize
        {
            get => (int)GetParameter(CompressParameterEnum.FL2_p_dictionarySize);
            set => SetParameter(CompressParameterEnum.FL2_p_dictionarySize, (nuint)value);
        }

        public int SearchDepth
        {
            get => (int)GetParameter(CompressParameterEnum.FL2_p_searchDepth);
            set => SetParameter(CompressParameterEnum.FL2_p_searchDepth, (nuint)value);
        }

        public int FastLength
        {
            get => (int)GetParameter(CompressParameterEnum.FL2_p_fastLength);
            set => SetParameter(CompressParameterEnum.FL2_p_fastLength, (nuint)value);
        }

        public Compressor(uint nbThreads = 0, int compressLevel = 6)
        {
            if (nbThreads == 1)
            {
                _context = NativeMethods.FL2_createCCtx();
            }
            _context = NativeMethods.FL2_createCCtxMt(nbThreads);
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
            nuint code = NativeMethods.FL2_compressCCtx(_context, buffer, (nuint)buffer.Length, src, (nuint)src.Length, compressLevel);
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
                    code = NativeMethods.FL2_compressCCtx(_context, accessorDst.mmPtr, code, accessorSrc.mmPtr, (nuint)sourceFile.Length, CompressLevel);
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

        public nuint SetParameter(CompressParameterEnum param, nuint value)
        {
            nuint code = NativeMethods.FL2_CCtx_setParameter(_context, param, value);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return code;
        }

        public nuint GetParameter(CompressParameterEnum param)
        {
            var code = NativeMethods.FL2_CCtx_getParameter(_context, param);
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
                NativeMethods.FL2_freeCCtx(_context);
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