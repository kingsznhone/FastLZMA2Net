using System.Buffers;
using System.IO.MemoryMappedFiles;

namespace FastLZMA2Net
{
    /// <summary>
    /// Provides static helpers for Fast LZMA2 compression and decompression.
    /// </summary>
    public static class FL2
    {
        #region Properties

        /// <summary>
        /// Gets the library version.
        /// </summary>
        public static readonly Version Version =
            typeof(FL2).Assembly.GetName().Version ?? new Version(0, 0, 0);

        /// <summary>
        /// Gets the version as a numeric code.
        /// </summary>
        public static int VersionNumber => Version.Major * 100 * 100 + Version.Minor * 100 + Version.Build;

        /// <summary>
        /// Gets the version as a dotted string.
        /// </summary>
        public static string VersionString => $"{Version.Major}.{Version.Minor}.{Version.Build}";

        /// <summary>
        /// Gets the maximum supported thread count.
        /// </summary>
        public const int MaxThreads = 200;

        /// <summary>
        /// Gets the minimum supported dictionary size in bytes.
        /// </summary>
        public const int DictSizeMin = 1 << 20;

        /// <summary>
        /// Gets the maximum supported dictionary size in bytes.
        /// </summary>
        public static int DictSizeMax => nint.Size == 4 ? 1 << 27 : 1 << 30;

        /// <summary>
        /// Gets the minimum supported block overlap.
        /// </summary>
        public const int BlockOverlapMin = 0;

        /// <summary>
        /// Gets the maximum supported block overlap.
        /// </summary>
        public const int BlockOverlapMax = 14;

        /// <summary>
        /// Gets the minimum supported reset interval.
        /// </summary>
        public const int ResetIntervalMin = 1;

        /// <summary>
        /// Gets the maximum supported reset interval.
        /// </summary>
        public const int ResetIntervalMax = 16;

        /// <summary>
        /// Gets the minimum supported buffer resize value.
        /// </summary>
        public const int BufferResizeMin = 0;

        /// <summary>
        /// Gets the maximum supported buffer resize value.
        /// </summary>
        public const int BufferResizeMax = 4;

        /// <summary>
        /// Gets the default buffer resize value.
        /// </summary>
        public const int BufferResizeDefault = 2;

        /// <summary>
        /// Gets the minimum supported chain log.
        /// </summary>
        public const int ChainLogMin = 4;

        /// <summary>
        /// Gets the maximum supported chain log.
        /// </summary>
        public const int ChainLogMax = 14;

        /// <summary>
        /// Gets the minimum supported hybrid cycle count.
        /// </summary>
        public const int HybridCyclesMin = 1;

        /// <summary>
        /// Gets the maximum supported hybrid cycle count.
        /// </summary>
        public const int HybridCyclesMax = 64;

        /// <summary>
        /// Gets the minimum supported search depth.
        /// </summary>
        public const int SearchDepthMin = 6;

        /// <summary>
        /// Gets the maximum supported search depth.
        /// </summary>
        public const int SearchDepthMax = 254;

        /// <summary>
        /// Gets the minimum supported fast length.
        /// </summary>
        public const int FastLengthMin = 6;

        /// <summary>
        /// Gets the maximum supported fast length.
        /// </summary>
        public const int FastLengthMax = 273;

        /// <summary>
        /// Gets the minimum supported literal context bits.
        /// </summary>
        public const int LCMin = 0;

        /// <summary>
        /// Gets the maximum supported literal context bits.
        /// </summary>
        public const int LCMax = 4;

        /// <summary>
        /// Gets the minimum supported literal position bits.
        /// </summary>
        public const int LPMin = 0;

        /// <summary>
        /// Gets the maximum supported literal position bits.
        /// </summary>
        public const int LPMax = 4;

        /// <summary>
        /// Gets the minimum supported position bits.
        /// </summary>
        public const int PBMin = 0;

        /// <summary>
        /// Gets the maximum supported position bits.
        /// </summary>
        public const int PBMax = 4;

        /// <summary>
        /// Gets the maximum supported combined lc/lp value.
        /// </summary>
        public const int LclpMax = 4;

        /// <summary>
        /// Gets the maximum compression level available.
        /// </summary>
        public static int CompressionLevelMax => NativeMethods.FL2_maxCLevel();

        /// <summary>
        /// Gets the maximum compression level available in high-compression mode.
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
        /// Returns the maximum compressed size in worst case scenario for the given source size.
        /// </summary>
        /// <param name="streamSize">Size of the source data in bytes.</param>
        /// <returns>Maximum compressed size in bytes.</returns>
        public static nuint FindCompressBound(nuint streamSize)
        {
            return NativeMethods.FL2_compressBound(streamSize);
        }

        /// <summary>
        /// Returns the maximum compressed size in worst case for the given source.
        /// </summary>
        /// <param name="src">Source data.</param>
        /// <returns>Maximum compressed size in bytes.</returns>
        public static nuint FindCompressBound(byte[] src)
        {
            ArgumentNullException.ThrowIfNull(src);
            return NativeMethods.FL2_compressBound((nuint)src.Length);
        }

        /// <summary>
        /// Returns the maximum compressed size in worst case for the given source.
        /// </summary>
        /// <param name="src">Source data.</param>
        /// <returns>Maximum compressed size in bytes.</returns>
        public static nuint FindCompressBound(ReadOnlySpan<byte> src)
            => NativeMethods.FL2_compressBound((nuint)src.Length);

        /// <summary>
        /// Find Decompressed Size of a Compressed Data
        /// </summary>
        /// <param name="data">Compressed Data</param>
        /// <returns>Decompressed Size</returns>
        /// <exception cref="FL2Exception">Thrown when the compressed data is corrupt or its size cannot be determined.</exception>
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
        public static nuint FindDecompressedSize(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            FileInfo file = new(filePath);
            if (!file.Exists)
            {
                throw new FileNotFoundException("File not found", filePath);
            }
            using (DirectFileAccessor accessor = new(filePath, FileMode.Open, null, file.Length, MemoryMappedFileAccess.Read))
            {
                var size = NativeMethods.FL2_findDecompressedSize(accessor.AsReadOnlySpan(), (nuint)file.Length);
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
        /// <param name="data">Source data.</param>
        /// <param name="level">Compression level.</param>
        /// <returns>Compressed byte array.</returns>
        public static byte[] Compress(ReadOnlySpan<byte> data, int level)
        {
            nuint bound = NativeMethods.FL2_compressBound((nuint)data.Length);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(checked((int)bound));
            try
            {
                nuint code = NativeMethods.FL2_compress(buffer, bound, data, (nuint)data.Length, level);
                if (FL2Exception.IsError(code))
                    throw new FL2Exception(code);
                return buffer.AsSpan(0, checked((int)code)).ToArray();
            }
            finally
            {
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
        /// <param name="data">Source data.</param>
        /// <param name="level">Compression level.</param>
        /// <param name="nbThreads">Number of threads to use; 0 selects all cores.</param>
        /// <returns>Compressed byte array.</returns>
        public static byte[] CompressMT(ReadOnlySpan<byte> data, int level, uint nbThreads)
        {
            nuint bound = NativeMethods.FL2_compressBound((nuint)data.Length);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(checked((int)bound));
            try
            {
                nuint code = NativeMethods.FL2_compressMt(buffer, bound, data, (nuint)data.Length, level, nbThreads);
                if (FL2Exception.IsError(code))
                    throw new FL2Exception(code);
                return buffer.AsSpan(0, checked((int)code)).ToArray();
            }
            finally
            {
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
        /// <param name="data">Compressed data.</param>
        /// <returns>Decompressed byte array.</returns>
        public static byte[] Decompress(ReadOnlySpan<byte> data)
        {
            nuint decompressedSize = NativeMethods.FL2_findDecompressedSize(data, (nuint)data.Length);
            if (FL2Exception.IsError(decompressedSize))
                throw new FL2Exception(decompressedSize);
            byte[] decompressed = new byte[checked((int)decompressedSize)];
            nuint code = NativeMethods.FL2_decompress(decompressed, decompressedSize, data, (nuint)data.Length);
            if (FL2Exception.IsError(code))
                throw new FL2Exception(code);
            return code == decompressedSize ? decompressed : decompressed[0..checked((int)code)];
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
        /// <param name="data">Compressed data.</param>
        /// <param name="nbThreads">Number of threads to use; 0 selects all cores.</param>
        /// <returns>Decompressed byte array.</returns>
        public static byte[] DecompressMT(ReadOnlySpan<byte> data, uint nbThreads)
        {
            nuint decompressedSize = NativeMethods.FL2_findDecompressedSize(data, (nuint)data.Length);
            if (FL2Exception.IsError(decompressedSize))
                throw new FL2Exception(decompressedSize);
            byte[] decompressed = new byte[checked((int)decompressedSize)];
            nuint code = NativeMethods.FL2_decompressMt(decompressed, decompressedSize, data, (nuint)data.Length, nbThreads);
            if (FL2Exception.IsError(code))
                throw new FL2Exception(code);
            return code == decompressedSize ? decompressed : decompressed[0..checked((int)code)];
        }

        /// <summary>
        /// Estimates memory usage for compression at the given level.
        /// </summary>
        /// <param name="compressionLevel">Compression level.</param>
        /// <param name="nbThreads">Number of threads to use; 0 selects all cores.</param>
        /// <returns>Estimated memory usage in bytes.</returns>
        public static nuint EstimateCompressMemoryUsage(int compressionLevel, uint nbThreads)
            => NativeMethods.FL2_estimateCCtxSize(compressionLevel, nbThreads);

        /// <summary>
        /// Estimates memory usage for compression with the given parameters.
        /// </summary>
        /// <param name="parameters">Compression parameters.</param>
        /// <param name="nbThreads">Number of threads to use; 0 selects all cores.</param>
        /// <returns>Estimated memory usage in bytes.</returns>
        public static nuint EstimateCompressMemoryUsage(CompressionParameters parameters, uint nbThreads)
            => NativeMethods.FL2_estimateCCtxSize_byParams(ref parameters, nbThreads);

        /// <summary>
        /// Estimates memory usage for compression using an existing context's settings.
        /// </summary>
        /// <param name="context">Compression context handle.</param>
        /// <returns>Estimated memory usage in bytes.</returns>
        public static nuint EstimateCompressMemoryUsage(nint context)
            => NativeMethods.FL2_estimateCCtxSize_usingCCtx(context);

        /// <summary>
        /// Estimates memory usage for decompression.
        /// </summary>
        /// <param name="nbThreads">Number of threads to use; 0 selects all cores.</param>
        /// <returns>Estimated memory usage in bytes.</returns>
        public static nuint EstimateDecompressMemoryUsage(uint nbThreads)
            => NativeMethods.FL2_estimateDCtxSize(nbThreads);

        /// <summary>
        /// Gets the dictionary size from a property byte.
        /// </summary>
        /// <param name="prop">Dictionary size property byte.</param>
        /// <returns>Dictionary size in bytes.</returns>
        public static nuint GetDictSizeFromProp(byte prop)
            => NativeMethods.FL2_getDictSizeFromProp(prop);
    }
}