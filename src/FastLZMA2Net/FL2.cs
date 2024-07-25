using System.Drawing;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FastLZMA2Net
{
    public static partial class FL2
    {
       
        #region Properties

        public static readonly Version Version = new Version(1, 0, 1);
        public static int VersionNumber => Version.Major * 100 * 100 + Version.Minor * 100 + Version.Build;
        public static string VersionString => $"{Version.Major}.{Version.Minor}.{Version.Build}";

        public static int MaxThreads => _maxThreads;
        public static int DictSizeMin => 1 << 20; //pow(2,20)
        public static int DictSizeMax => nint.Size == 4 ? 1 << 27 : 1 << 30; //pow(2,30)

        public static int BlockOverlapMin => 0;
        public static int BlockOverlapMax => 14;

        public static int ResetIntervalMin => 1;
        public static int ResetIntervalMax => 16;

        public static int BufferResizeMin => 0;
        public static int BufferResizeMax => 4;
        public static int BufferResizeDefault => 2;

        public static int ChainLogMin => 4;
        public static int ChainLogMax => 14;

        public static int HybridCyclesMin => 1;
        public static int HybridCyclesMax => 64;

        public static int SearchDepthMin => 6;
        public static int SearchDepthMax => 254;

        public static int FastLengthMin => 6;
        public static int FastLengthMax => 273;

        public static int LCMin => 0;
        public static int LCMax => 4;

        public static int LPMin => 0;
        public static int LPMax => 4;

        public static int PBMin => 0;
        public static int PBMax => 4;
        public static int LCLP_MAX => 4;

        /// <summary>
        /// maximum compression level available
        /// </summary>
        public static int MaxCompressionLevel =>ExternMethods.FL2_maxCLevel();

        /// <summary>
        /// maximum compression level available in high mode
        /// </summary>
        public static int MaxHighCompressionLevel => ExternMethods.FL2_maxHighCLevel();

        #endregion Properties

        private const int _maxThreads = 200;

        public static CompressionParameters GetPresetLevelParameters(int level, int high)
        {
            CompressionParameters parameters = new();
            nuint code = ExternMethods.FL2_getLevelParameters(level, high, ref parameters);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return parameters;
        }

        /// <summary>
        /// maximum compressed size in worst case scenario
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static nuint FindCompressBound(byte[] src)
        {
            return ExternMethods.FL2_compressBound((nuint)src.Length);
        }

        /// <summary>
        /// Find Decompressed Size of a Compressed Data
        /// </summary>
        /// <param name="data">Compressed Data</param>
        /// <returns>Decompressed Size</returns>
        /// <exception cref="Exception"></exception>
        public static nuint FindDecompressedSize(byte[] data)
        {
            var ContentSizeError = nuint.MaxValue;
            nuint size = ExternMethods.FL2_findDecompressedSize(data, (nuint)data.LongLength);
            if (size == ContentSizeError)
            {
                throw new FL2Exception(size);
            }
            return size;
        }

        public static unsafe nuint FindDecompressedSize(string filePath)
        {
            FileInfo localFile = new FileInfo(filePath);
            MemoryMappedFile mmFile = MemoryMappedFile.CreateFromFile(localFile.FullName, FileMode.Open);
            nuint size = nuint.Zero;

            using (var accessor = mmFile.CreateViewAccessor())
            {
                byte* start = null;
                accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref start);
                try
                {
                    size = ExternMethods.FL2_findDecompressedSize(start, (nuint)localFile.Length);
                    if (size == nuint.MaxValue)
                    {
                        throw new FL2Exception(size);
                    }
                    return size;
                }
                finally
                {
                    accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                }
            }
        }
        public static byte[] Compress(byte[] data, int Level)
        {
            byte[] compressed = new byte[ExternMethods.FL2_compressBound((nuint)data.Length)];
            nuint code = ExternMethods.FL2_compress(compressed, (nuint)compressed.Length, data, (nuint)data.Length, Level);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return compressed[..(int)code];
        }

        public static byte[] CompressMT(byte[] data, int Level, uint nbThreads)
        {
            byte[] compressed = new byte[   ExternMethods.FL2_compressBound((nuint)data.Length)];
            nuint code = ExternMethods.FL2_compressMt(compressed, (nuint)compressed.Length, data, (nuint)data.Length, Level, nbThreads);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return compressed;
        }

        public static byte[] Decompress(byte[] data)
        {
            nuint decompressedSize = ExternMethods.FL2_findDecompressedSize(data, (nuint)data.Length);
            byte[] decompressed = new byte[decompressedSize];
            nuint code = ExternMethods.FL2_decompress(decompressed, decompressedSize, data, (nuint)data.Length);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return decompressed;
        }

        public static byte[] DecompressMT(byte[] data, uint nbThreads)
        {
            nuint decompressedSize = ExternMethods.FL2_findDecompressedSize(data, (nuint)data.Length);
            byte[] decompressed = new byte[decompressedSize];
            nuint code = ExternMethods.FL2_decompressMt(decompressed, decompressedSize, data, (nuint)data.Length, nbThreads);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return decompressed;
        }

        public static nuint EstimateCompressMemoryUsage(int compressionLevel, uint nbThreads)
            => ExternMethods.FL2_estimateCCtxSize(compressionLevel, nbThreads);

        public static nuint EstimateCompressMemoryUsage(CompressionParameters parameters, uint nbThreads)
            => ExternMethods.FL2_estimateCCtxSize_byParams(ref parameters, nbThreads);

        public static nuint EstimateCompressMemoryUsage(IntPtr context)
            => ExternMethods.FL2_estimateCCtxSize_usingCCtx(context);

        public static nuint EstimateDecompressMemoryUsage(uint nbThreads)
            => ExternMethods.FL2_estimateDCtxSize(nbThreads);

        public static nuint GetDictSizeFromProp(byte prop)
            => ExternMethods.FL2_getDictSizeFromProp(prop);
    }
}