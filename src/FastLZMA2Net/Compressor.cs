using System.IO.MemoryMappedFiles;
using System.Text.RegularExpressions;

namespace FastLZMA2Net
{
    /// <summary>
    /// Fast LZMA2 Compress Context
    /// </summary>
    public partial class Compressor : IDisposable
    {
        private readonly nint _CContext;
        private bool disposed = false;
        private bool disposedValue;

        /// <summary>
        /// Thread use of the context
        /// </summary>
        public uint ThreadCount => NativeMethods.FL2_getCCtxThreadCount(_CContext);

        /// <summary>
        /// Dictionary size property
        /// </summary>
        public byte DictSizeProperty => NativeMethods.FL2_getCCtxDictProp(_CContext);

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
        /// Dictionary size with FL2.DictSizeMin & FL2.DictSizeMax
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
        /// <param name="nbThreads">How many thread use. auto = 0</param>
        /// <param name="compressLevel">default = 6</param>
        public Compressor(uint nbThreads = 0, int compressLevel = 6)
        {
            _CContext = NativeMethods.FL2_createCCtxMt(nbThreads);
            CompressLevel = (int)compressLevel;
        }

        /// <summary>
        /// Compress data asynchronized
        /// </summary>
        /// <param name="src">Data byte array</param>
        /// <returns>Bytes Compressed</returns>
        public Task<byte[]> CompressAsync(byte[] src)
        {
            return CompressAsync(src, CompressLevel);
        }

        /// <summary>
        /// Compress data asynchronized
        /// </summary>
        /// <param name="src">Data byte array</param>
        /// <param name="compressLevel">compress level</param>
        /// <returns>Bytes Compressed</returns>
        public Task<byte[]> CompressAsync(byte[] src, int compressLevel)
        {
            return Task.Run(() => Compress(src, compressLevel));
        }

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
        /// <exception cref="FL2Exception"></exception>
        public byte[] Compress(byte[] src, int compressLevel)
        {
            ArgumentNullException.ThrowIfNull(src);

            byte[] buffer = new byte[FL2.FindCompressBound(src)];
            nuint code = NativeMethods.FL2_compressCCtx(_CContext, buffer, (nuint)buffer.Length, src, (nuint)src.Length, compressLevel);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return buffer[..(int)code];
        }

        /// <summary>
        /// Compress file using direct file access. No memory copy overhead.
        /// </summary>
        /// <param name="srcPath">source file path</param>
        /// <param name="dstPath">output file path</param>
        /// <returns>Bytes Compressed</returns>
        /// <exception cref="FL2Exception"></exception>
        public unsafe nuint Compress(string srcPath, string dstPath)
        {
            nuint code;
            FileInfo sourceFile = new FileInfo(srcPath);
            FileInfo destFile = new FileInfo(dstPath);
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

        /// <summary>
        /// Set detail compress parameter
        /// </summary>
        /// <param name="param"> Parameter Enum</param>
        /// <param name="value"></param>
        /// <returns>Error Code</returns>
        /// <exception cref="FL2Exception"></exception>
        public nuint SetParameter(FL2Parameter param, nuint value)
        {
            nuint code = NativeMethods.FL2_CCtx_setParameter(_CContext, param, value);
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