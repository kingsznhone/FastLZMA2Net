using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FastLZMA2Net
{
    public static unsafe partial class NativeMethods
    {
        static NativeMethods()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                SetWinDllDirectory();
            else
            {
                throw new PlatformNotSupportedException($"{Environment.OSVersion.Platform} is not support");
            }
        }

        public static void SetWinDllDirectory()
        {
            string path;

            var location = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(location) || (path = Path.GetDirectoryName(location)) == null)
            {
                Trace.TraceWarning($"{nameof(FastLZMA2Net)}: Failed to get executing assembly location");
                return;
            }

            // Nuget package
            if (Path.GetFileName(path).StartsWith("net", StringComparison.Ordinal) && Path.GetFileName(Path.GetDirectoryName(path)) == "lib" && File.Exists(Path.Combine(path, "../../fastlzma2net.nuspec")))
                path = Path.Combine(path, "../../build");

            var platform = Environment.Is64BitProcess ? "x64" : "x86";
            if (!SetDllDirectoryW(Path.Combine(path, platform)))
                Trace.TraceWarning($"{nameof(FastLZMA2Net)}: Failed to set DLL directory to '{path}'");
        }

        [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetDllDirectoryW(string path);

        private const string LibraryName = "fast-lzma2";

        #region Simple Function

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_compress(byte[] dst, nuint dstCapacity, byte[] src, nuint srcSize, int compressionLevel);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_compressMt(byte[] dst, nuint dstCapacity, byte[] src, nuint srcSize, int compressionLevel, uint nbThreads);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_decompress(byte[] dst, nuint dstCapacity, byte[] src, nuint compressedSize);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_decompressMt(byte[] dst, nuint dstCapacity, byte[] src, nuint compressedSize, uint nbThreads);

        #endregion Simple Function

        #region Helper Functions

        /// <summary>
        /// A property byte is assumed to exist at position 0 in `src`. If the stream was created without one,  subtract 1 byte from `src` when passing it to the function.
        /// </summary>
        /// <param name="src">should point to the start of a LZMA2 encoded stream</param>
        /// <param name="srcSize">must be at least as large as the LZMA2 stream including end marker.</param>
        /// <returns>
        /// decompressed size of the stream in `src`, if known.
        /// FL2_CONTENTSIZE_ERROR (nuint.max) if an error occurred (e.g. corruption, srcSize too small)
        /// </returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_findDecompressedSize(byte[] src, nuint srcSize);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_findDecompressedSize(byte* src, nuint srcSize);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_getDictSizeFromProp(byte prop);

        /// <summary>
        /// maximum compressed size in worst case scenario
        /// </summary>
        /// <param name="srcSize"></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_compressBound(nuint srcSize);

        /// <summary>
        /// maximum compression level available
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int FL2_maxCLevel();

        /// <summary>
        /// maximum compression level available in high mode
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int FL2_maxHighCLevel();

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_getLevelParameters(int compressionLevel, int high, ref CompressionParameters parameters);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_estimateCCtxSize(int compressionLevel, uint nbThreads);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_estimateCCtxSize_byParams(ref CompressionParameters parameters, uint nbThreads);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_estimateCCtxSize_usingCCtx(IntPtr context);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_estimateDCtxSize(uint nbThreads);

        #endregion Helper Functions

        #region Compress Context

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint FL2_createCCtx();

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint FL2_createCCtxMt(uint nbThreads);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint FL2_freeCCtx(nint context);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial uint FL2_getCCtxThreadCount(nint context);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_compressCCtx(nint context,
                                                      byte[] dst, nuint dstCapacity,
                                                      byte[] src, nuint srcSize,
                                                      int compressionLevel);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_compressCCtx(nint context,
                                              byte* dst, nuint dstCapacity,
                                              byte* src, nuint srcSize,
                                              int compressionLevel);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial byte FL2_getCCtxDictProp(nint context);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_CCtx_setParameter(nint context, CompressParameterEnum param, nuint value);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_CCtx_getParameter(nint context, CompressParameterEnum param);

        #endregion Compress Context

        #region Decompress Context

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint FL2_createDCtx();

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint FL2_createDCtxMt(uint nbThreads);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint FL2_freeDCtx(nint context);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial uint FL2_getDCtxThreadCount(nint context);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_initDCtx(nint context, byte prop);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_decompressDCtx(nint context,
                                                        byte[] dst, nuint dstCapacity,
                                                        byte[] src, nuint srcSize);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_decompressDCtx(nint context,
                                                byte* dst, nuint dstCapacity,
                                                byte* src, nuint srcSize);

        #endregion Decompress Context

        #region Compress Stream

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint FL2_createCStream();

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint FL2_createCStreamMt(uint nbThreads, int dualBuffer);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void FL2_freeCStream(nint fcs);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_initCStream(nint fcs, int compressionLevel);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_setCStreamTimeout(nint fcs, uint timeout);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_compressStream(nint fcs, ref FL2OutBuffer output, ref FL2InBuffer input);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_copyCStreamOutput(nint fcs, ref FL2OutBuffer output);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_getDictionaryBuffer(nint fcs, ref FL2DictBuffer dict);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_updateDictionary(nint fcs, nuint addedSize);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_getNextCompressedBuffer(nint fcs, ref FL2cBuffer cbuf);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_getCStreamProgress(nint fcs, ulong outputSize);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_waitCStream(nint fcs);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void FL2_cancelCStream(nint fcs);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_remainingOutputSize(nint fcs);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_flushStream(nint fcs, ref FL2OutBuffer output);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_endStream(nint fcs, ref FL2OutBuffer output);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_CStream_setParameter(nint fcs, CompressParameterEnum param, nuint value);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_CStream_getParameter(nint fcs, CompressParameterEnum param);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_estimateCStreamSize(int compressionLevel, uint nbThreads, int dualBuffer);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_estimateCStreamSize_byParams(ref CompressionParameters parameters, uint nbThreads, int dualBuffer);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_estimateCStreamSize_usingCStream(nint fcs);

        #endregion Compress Stream

        #region Decompress Stream

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint FL2_createDStream();

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint FL2_createDStreamMt(uint nbThreads);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_freeDStream(nint fds);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void FL2_setDStreamMemoryLimitMt(nint fds, nuint limit);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_setDStreamTimeout(nint fds, uint timeout);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_waitDStream(nint fds);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void FL2_cancelDStream(nint fds);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial ulong FL2_getDStreamProgress(nint fds);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_initDStream(nint fds);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_initDStream_withProp(nint fds, byte prop);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_decompressStream(nint fds, ref FL2OutBuffer output, ref FL2InBuffer input);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint FL2_estimateDStreamSize(nuint dictSize, uint nbThreads);

        #endregion Decompress Stream
    }
}