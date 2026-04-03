using System.Buffers;
using System.IO.MemoryMappedFiles;

namespace FastLZMA2Net
{
    public static class FL2
    {
        #region Properties

        public static readonly Version Version =
            typeof(FL2).Assembly.GetName().Version!;
        public static int VersionNumber => Version.Major * 100 * 100 + Version.Minor * 100 + Version.Build;
        public static string VersionString => $"{Version.Major}.{Version.Minor}.{Version.Build}";
        public const int MaxThreads = 200;
        public const int DictSizeMin = 1 << 20;
        public static int DictSizeMax => nint.Size == 4 ? 1 << 27 : 1 << 30;

        public const int BlockOverlapMin = 0;
        public const int BlockOverlapMax = 14;

        public const int ResetIntervalMin = 1;
        public const int ResetIntervalMax = 16;

        public const int BufferResizeMin = 0;
        public const int BufferResizeMax = 4;
        public const int BufferResizeDefault = 2;

        public const int ChainLogMin = 4;
        public const int ChainLogMax = 14;

        public const int HybridCyclesMin = 1;
        public const int HybridCyclesMax = 64;

        public const int SearchDepthMin = 6;
        public const int SearchDepthMax = 254;

        public const int FastLengthMin = 6;
        public const int FastLengthMax = 273;

        public const int LCMin = 0;
        public const int LCMax = 4;

        public const int LPMin = 0;
        public const int LPMax = 4;

        public const int PBMin = 0;
        public const int PBMax = 4;
        public const int LclpMax = 4;

        /// <summary>
        /// maximum compression level available
        /// </summary>
        public static int CompressionLevelMax => NativeMethods.FL2_maxCLevel();

        /// <summary>
        /// maximum compression level available in high mode
        /// </summary>
        public static int HighCompressionLevelMax => NativeMethods.FL2_maxHighCLevel();

        #endregion Properties

        /// <summary>
        /// Gets compression parameters defined by a preset compression level.
        /// </summary>
        /// <param name="level">Preset compression level.</param>
        /// <param name="high">Set to 1 to use high compression preset.</param>
        /// <returns>The compression parameters for the given level.</returns>
        /// <exception cref="FL2Exception">Thrown when the level is out of range.</exception>
        public static CompressionParameters GetPresetLevelParameters(int level, int high)
        {
            CompressionParameters parameters = new();
            nuint code = NativeMethods.FL2_getLevelParameters(level, high, ref parameters);
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
        ///
        public static nuint FindCompressBound(nuint streamSize)
        {
            return NativeMethods.FL2_compressBound(streamSize);
        }

        /// <summary>
        /// Returns the maximum compressed size in worst case for the given source.
        /// </summary>
        public static nuint FindCompressBound(byte[] src)
        {
            ArgumentNullException.ThrowIfNull(src);
            return NativeMethods.FL2_compressBound((nuint)src.Length);
        }

        /// <summary>
        /// Returns the maximum compressed size in worst case for the given source.
        /// </summary>
        public static nuint FindCompressBound(ReadOnlySpan<byte> src)
            => NativeMethods.FL2_compressBound((nuint)src.Length);

        /// <summary>
        /// Find Decompressed Size of a Compressed Data
        /// </summary>
        /// <param name="data">Compressed Data</param>
        /// <returns>Decompressed Size</returns>
        /// <exception cref="Exception"></exception>
        public static nuint FindDecompressedSize(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);
            var ContentSizeError = nuint.MaxValue;
            nuint size = NativeMethods.FL2_findDecompressedSize(data, (nuint)data.LongLength);
            if (size == ContentSizeError)
            {
                throw new FL2Exception(size);
            }
            return size;
        }

        /// <summary>
        /// Finds the decompressed size of a compressed file.
        /// </summary>
        /// <param name="filePath">Path to the compressed file.</param>
        /// <returns>Decompressed size in bytes.</returns>
        /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
        /// <exception cref="FL2Exception">Thrown when the file is corrupt.</exception>
        public static unsafe nuint FindDecompressedSize(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            FileInfo file = new FileInfo(filePath);
            if (!file.Exists)
            {
                throw new FileNotFoundException("File not found", filePath);
            }
            using (DirectFileAccessor accessor = new DirectFileAccessor(filePath, FileMode.Open, null, file.Length, MemoryMappedFileAccess.Read))
            {
                var size = NativeMethods.FL2_findDecompressedSize(accessor.mmPtr, (nuint)file.Length);
                if (size == nuint.MaxValue)
                {
                    throw new FL2Exception(size);
                }
                return size;
            }
        }

        /// <summary>
        /// Compresses data using the specified compression level.
        /// </summary>
        /// <param name="data">Source data to compress.</param>
        /// <param name="level">Compression level.</param>
        /// <returns>Compressed byte array.</returns>
        /// <exception cref="FL2Exception">Thrown when compression fails.</exception>
        public static byte[] Compress(byte[] data, int level)
        {
            ArgumentNullException.ThrowIfNull(data);
            nuint bound = NativeMethods.FL2_compressBound((nuint)data.Length);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(checked((int)bound));
            try
            {
                nuint code = NativeMethods.FL2_compress(buffer, bound, data, (nuint)data.Length, level);
                if (FL2Exception.IsError(code))
                {
                    throw new FL2Exception(code);
                }
                return buffer.AsSpan(0, checked((int)code)).ToArray();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Compresses data from a <see cref="ReadOnlySpan{T}"/> source.
        /// </summary>
        public static unsafe byte[] Compress(ReadOnlySpan<byte> data, int level)
        {
            nuint bound = NativeMethods.FL2_compressBound((nuint)data.Length);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(checked((int)bound));
            nint ctx = NativeMethods.FL2_createCCtx();
            if (ctx == IntPtr.Zero)
                throw new FL2Exception(FL2ErrorCode.MemoryAllocation);
            try
            {
                nuint code;
                fixed (byte* pSrc = data)
                fixed (byte* pDst = buffer)
                {
                    code = NativeMethods.FL2_compressCCtx(ctx, pDst, bound, pSrc, (nuint)data.Length, level);
                }
                if (FL2Exception.IsError(code))
                    throw new FL2Exception(code);
                return buffer.AsSpan(0, checked((int)code)).ToArray();
            }
            finally
            {
                NativeMethods.FL2_freeCCtx(ctx);
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Compresses data using multiple threads.
        /// </summary>
        /// <param name="data">Source data to compress.</param>
        /// <param name="level">Compression level.</param>
        /// <param name="nbThreads">Number of threads (0 = all cores).</param>
        /// <returns>Compressed byte array.</returns>
        /// <exception cref="FL2Exception">Thrown when compression fails.</exception>
        public static byte[] CompressMT(byte[] data, int level, uint nbThreads)
        {
            ArgumentNullException.ThrowIfNull(data);
            nuint bound = NativeMethods.FL2_compressBound((nuint)data.Length);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(checked((int)bound));
            try
            {
                nuint code = NativeMethods.FL2_compressMt(buffer, bound, data, (nuint)data.Length, level, nbThreads);
                if (FL2Exception.IsError(code))
                {
                    throw new FL2Exception(code);
                }
                return buffer.AsSpan(0, checked((int)code)).ToArray();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Compresses data from a <see cref="ReadOnlySpan{T}"/> source using multiple threads.
        /// </summary>
        public static unsafe byte[] CompressMT(ReadOnlySpan<byte> data, int level, uint nbThreads)
        {
            nuint bound = NativeMethods.FL2_compressBound((nuint)data.Length);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(checked((int)bound));
            nint ctx = NativeMethods.FL2_createCCtxMt(nbThreads);
            if (ctx == IntPtr.Zero)
                throw new FL2Exception(FL2ErrorCode.MemoryAllocation);
            try
            {
                nuint code;
                fixed (byte* pSrc = data)
                fixed (byte* pDst = buffer)
                {
                    code = NativeMethods.FL2_compressCCtx(ctx, pDst, bound, pSrc, (nuint)data.Length, level);
                }
                if (FL2Exception.IsError(code))
                    throw new FL2Exception(code);
                return buffer.AsSpan(0, checked((int)code)).ToArray();
            }
            finally
            {
                NativeMethods.FL2_freeCCtx(ctx);
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Decompresses a single LZMA2 compressed stream.
        /// </summary>
        /// <param name="data">Compressed data.</param>
        /// <returns>Decompressed byte array.</returns>
        /// <exception cref="FL2Exception">Thrown when decompression fails.</exception>
        public static byte[] Decompress(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);
            nuint decompressedSize = NativeMethods.FL2_findDecompressedSize(data, (nuint)data.Length);
            if (FL2Exception.IsError(decompressedSize))
            {
                throw new FL2Exception(decompressedSize);
            }
            byte[] decompressed = new byte[checked((int)decompressedSize)];
            nuint code = NativeMethods.FL2_decompress(decompressed, decompressedSize, data, (nuint)data.Length);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return code == decompressedSize ? decompressed : decompressed[0..checked((int)code)];
        }

        /// <summary>
        /// Decompresses data from a <see cref="ReadOnlySpan{T}"/> source.
        /// </summary>
        public static unsafe byte[] Decompress(ReadOnlySpan<byte> data)
        {
            nuint decompressedSize;
            fixed (byte* pSrc = data)
            {
                decompressedSize = NativeMethods.FL2_findDecompressedSize(pSrc, (nuint)data.Length);
            }
            if (FL2Exception.IsError(decompressedSize))
                throw new FL2Exception(decompressedSize);
            byte[] decompressed = new byte[checked((int)decompressedSize)];
            nint ctx = NativeMethods.FL2_createDCtx();
            if (ctx == IntPtr.Zero)
                throw new FL2Exception(FL2ErrorCode.MemoryAllocation);
            try
            {
                nuint code;
                fixed (byte* pSrc = data)
                fixed (byte* pDst = decompressed)
                {
                    code = NativeMethods.FL2_decompressDCtx(ctx, pDst, decompressedSize, pSrc, (nuint)data.Length);
                }
                if (FL2Exception.IsError(code))
                    throw new FL2Exception(code);
                return code == decompressedSize ? decompressed : decompressed[0..checked((int)code)];
            }
            finally
            {
                NativeMethods.FL2_freeDCtx(ctx);
            }
        }

        /// <summary>
        /// Decompresses data using multiple threads.
        /// </summary>
        /// <param name="data">Compressed data.</param>
        /// <param name="nbThreads">Number of threads (0 = all cores).</param>
        /// <returns>Decompressed byte array.</returns>
        /// <exception cref="FL2Exception">Thrown when decompression fails.</exception>
        public static byte[] DecompressMT(byte[] data, uint nbThreads)
        {
            ArgumentNullException.ThrowIfNull(data);
            nuint decompressedSize = NativeMethods.FL2_findDecompressedSize(data, (nuint)data.Length);
            if (FL2Exception.IsError(decompressedSize))
                throw new FL2Exception(decompressedSize);
            byte[] decompressed = new byte[checked((int)decompressedSize)];
            nuint code = NativeMethods.FL2_decompressMt(decompressed, decompressedSize, data, (nuint)data.Length, nbThreads);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return code == decompressedSize ? decompressed : decompressed[0..checked((int)code)];
        }

        /// <summary>
        /// Decompresses data from a <see cref="ReadOnlySpan{T}"/> source using multiple threads.
        /// </summary>
        public static unsafe byte[] DecompressMT(ReadOnlySpan<byte> data, uint nbThreads)
        {
            nuint decompressedSize;
            fixed (byte* pSrc = data)
            {
                decompressedSize = NativeMethods.FL2_findDecompressedSize(pSrc, (nuint)data.Length);
            }
            if (FL2Exception.IsError(decompressedSize))
                throw new FL2Exception(decompressedSize);
            byte[] decompressed = new byte[checked((int)decompressedSize)];
            nint ctx = NativeMethods.FL2_createDCtxMt(nbThreads);
            if (ctx == IntPtr.Zero)
                throw new FL2Exception(FL2ErrorCode.MemoryAllocation);
            try
            {
                nuint code;
                fixed (byte* pSrc = data)
                fixed (byte* pDst = decompressed)
                {
                    code = NativeMethods.FL2_decompressDCtx(ctx, pDst, decompressedSize, pSrc, (nuint)data.Length);
                }
                if (FL2Exception.IsError(code))
                    throw new FL2Exception(code);
                return code == decompressedSize ? decompressed : decompressed[0..checked((int)code)];
            }
            finally
            {
                NativeMethods.FL2_freeDCtx(ctx);
            }
        }

        /// <summary>
        /// Estimates memory usage for compression at the given level.
        /// </summary>
        public static nuint EstimateCompressMemoryUsage(int compressionLevel, uint nbThreads)
            => NativeMethods.FL2_estimateCCtxSize(compressionLevel, nbThreads);

        /// <summary>
        /// Estimates memory usage for compression with the given parameters.
        /// </summary>
        public static nuint EstimateCompressMemoryUsage(CompressionParameters parameters, uint nbThreads)
            => NativeMethods.FL2_estimateCCtxSize_byParams(ref parameters, nbThreads);

        /// <summary>
        /// Estimates memory usage for compression using an existing context's settings.
        /// </summary>
        public static nuint EstimateCompressMemoryUsage(nint context)
            => NativeMethods.FL2_estimateCCtxSize_usingCCtx(context);

        /// <summary>
        /// Estimates memory usage for decompression.
        /// </summary>
        public static nuint EstimateDecompressMemoryUsage(uint nbThreads)
            => NativeMethods.FL2_estimateDCtxSize(nbThreads);

        /// <summary>
        /// Gets the dictionary size from a property byte.
        /// </summary>
        public static nuint GetDictSizeFromProp(byte prop)
            => NativeMethods.FL2_getDictSizeFromProp(prop);
    }
}