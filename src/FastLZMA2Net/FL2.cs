using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FastLZMA2Net
{
    public static partial class FL2
    {
        const string LibraryName = "fast-lzma2";
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial nuint FL2_compress(byte[] dst, nuint dstCapacity, byte[] src, nuint srcSize, int compressionLevel);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial nuint FL2_compressMt(byte[] dst, nuint dstCapacity, byte[] src, nuint srcSize, int compressionLevel, uint nbThreads);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial nuint FL2_decompress(byte[] dst, nuint dstCapacity, byte[] src, nuint compressedSize);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial nuint FL2_decompressMt(byte[] dst, nuint dstCapacity, byte[] src, nuint compressedSize, uint nbThreads);

        /// <summary>
        /// A property byte is assumed to exist at position 0 in `src`. If the stream was created without one,  subtract 1 byte from `src` when passing it to the function.
        /// </summary>
        /// <param name="src">should point to the start of a LZMA2 encoded stream</param>
        /// <param name="srcSize">must be at least as large as the LZMA2 stream including end marker.</param>
        /// <returns>
        /// decompressed size of the stream in `src`, if known.
        /// FL2_CONTENTSIZE_ERROR (nuint.max) if an error occurred (e.g. corruption, srcSize too small)
        /// </returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial nuint FL2_findDecompressedSize(byte[] src, nuint srcSize);

        #region Helper Functions

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial nuint L2_getDictSizeFromProp(byte prop);

        /// <summary>
        /// maximum compressed size in worst case scenario
        /// </summary>
        /// <param name="srcSize"></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial nuint FL2_compressBound(nuint srcSize);

        /// <summary>
        /// maximum compression level available
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial int FL2_maxCLevel();

        /// <summary>
        /// maximum compression level available in high mode
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial int FL2_maxHighCLevel();

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial nuint FL2_getLevelParameters(int compressionLevel, int high, ref CompressionParameters parameters);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial nuint FL2_estimateCCtxSize(int compressionLevel, uint nbThreads);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial nuint FL2_estimateCCtxSize_byParams(ref CompressionParameters parameters, uint nbThreads);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial nuint FL2_estimateCCtxSize_usingCCtx(IntPtr context);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial nuint FL2_estimateDCtxSize(uint nbThreads);

        #endregion Helper Functions

        #region Properties

        public static readonly Version Version = new Version(1, 0, 1);
        public static int VersionNumber => Version.Major * 100 * 100 + Version.Minor * 100 + Version.Build;
        public static string VersionString => $"{Version.Major}.{Version.Minor}.{Version.Build}";

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
        public static int MaxCompressionLevel => FL2_maxCLevel();

        /// <summary>
        /// maximum compression level available in high mode
        /// </summary>
        public static int MaxHighCompressionLevel => FL2_maxHighCLevel();

        #endregion Properties

        private const int _maxThreads = 200;

        public static CompressionParameters GetPresetLevelParameters(int level, int high)
        {
            CompressionParameters parameters = new();
            nuint code = FL2_getLevelParameters(level, high, ref parameters);
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
        public static nuint CompressBound(byte[] src)
        {
            return FL2_compressBound((nuint)src.Length);
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
            nuint size = FL2_findDecompressedSize(data, (nuint)data.LongLength);
            if (size == ContentSizeError)
            {
                throw new FL2Exception(size);
            }
            return size;
        }

        public static byte[] Compress(byte[] data, int Level)
        {
            byte[] compressed = new byte[FL2_compressBound((nuint)data.Length)];
            nuint code = FL2_compress(compressed, (nuint)compressed.Length, data, (nuint)data.Length, Level);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return compressed[..(int)code];
        }

        public static byte[] CompressMT(byte[] data, int Level, uint nbThreads)
        {
            byte[] compressed = new byte[FL2_compressBound((nuint)data.Length)];
            nuint code = FL2_compressMt(compressed, (nuint)compressed.Length, data, (nuint)data.Length, Level, nbThreads);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return compressed;
        }

        public static byte[] Decompress(byte[] data)
        {
            nuint decompressedSize = FL2_findDecompressedSize(data, (nuint)data.Length);
            byte[] decompressed = new byte[decompressedSize];
            nuint code = FL2_decompress(decompressed, decompressedSize, data, (nuint)data.Length);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return decompressed;
        }

        public static byte[] DecompressMT(byte[] data, uint nbThreads)
        {
            nuint decompressedSize = FL2_findDecompressedSize(data, (nuint)data.Length);
            byte[] decompressed = new byte[decompressedSize];
            nuint code = FL2_decompressMt(decompressed, decompressedSize, data, (nuint)data.Length, nbThreads);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return decompressed;
        }

        public static nuint EstimateCompressMemoryUsage(int compressionLevel, uint nbThreads)
            => FL2_estimateCCtxSize(compressionLevel, nbThreads);

        public static nuint EstimateCompressMemoryUsage(CompressionParameters parameters, uint nbThreads)
            => FL2_estimateCCtxSize_byParams(ref parameters, nbThreads);

        public static nuint EstimateCompressMemoryUsage(IntPtr context)
            => FL2_estimateCCtxSize_usingCCtx(context);

        public static nuint EstimateDecompressMemoryUsage(uint nbThreads)
            => FL2_estimateDCtxSize(nbThreads);

        public static nuint GetDictSizeFromProp(byte prop)
            => L2_getDictSizeFromProp(prop);
    }
}