using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Security.Cryptography;
using System.Threading;
using System.Dynamic;
using System.IO;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Threading.Channels;
using System;
using System.Runtime.Intrinsics.X86;
using Microsoft.VisualBasic;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;
using System.Collections;
using System.ComponentModel;
using System.Data.Common;
using System.Drawing;
using System.Text;
using System.Timers;
using System.Reflection.Emit;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Intrinsics.Arm;
using System.Numerics;

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
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetDllDirectoryW(string path);

        private const string LibraryName = "fast-lzma2";

        #region Simple Function
        /// <summary>
        /// Compresses `src` content as a single LZMA2 compressed stream into already allocated `dst`.
        /// Call FL2_compressMt() to use > 1 thread.Specify nbThreads = 0 to use all cores.
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="dstCapacity"></param>
        /// <param name="src"></param>
        /// <param name="srcSize"></param>
        /// <param name="compressionLevel"></param>
        /// <returns> compressed size written into `dst` ,or an error code if it fails. </returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [return: MarshalAs(UnmanagedType.SysUInt)]
        internal static partial nuint FL2_compress([In] byte[] dst, nuint dstCapacity,[In] byte[] src, nuint srcSize, int compressionLevel);

        /// <summary>
        /// Compresses `src` content as a single LZMA2 compressed stream into already allocated `dst`.
        /// Call FL2_compressMt() to use > 1 thread.Specify nbThreads = 0 to use all cores.
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="dstCapacity"></param>
        /// <param name="src"></param>
        /// <param name="srcSize"></param>
        /// <param name="compressionLevel"></param>
        /// <param name="nbThreads"></param>
        /// <returns> compressed size written into `dst` ,or an error code if it fails. </returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_compressMt([In]byte[] dst, nuint dstCapacity,[In] byte[] src, nuint srcSize, int compressionLevel, uint nbThreads);

        /// <summary>
        /// Decompresses a single LZMA2 compressed stream from `src` into already allocated `dst`.
        /// `compressedSize` : must be at least the size of the LZMA2 stream.
        /// `dstCapacity` is the original, uncompressed size to regenerate, returned by calling FL2_findDecompressedSize().
        /// Call FL2_decompressMt() to use > 1 thread. Specify nbThreads = 0 to use all cores. 
        /// The stream must contain dictionary resets to use multiple threads.
        /// These are inserted during compression by default. 
        /// The frequency can be changed/disabled with the FL2_p_resetInterval parameter setting.
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="dstCapacity"></param>
        /// <param name="src"></param>
        /// <param name="compressedSize"></param>
        /// <returns>the number of bytes decompressed into `dst`,or an errorCode if it fails.</returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_decompress([In]byte[] dst, nuint dstCapacity,[In] byte[] src, nuint compressedSize);

        /// <summary>
        /// Decompresses a single LZMA2 compressed stream from `src` into already allocated `dst`.
        /// `compressedSize` : must be at least the size of the LZMA2 stream.
        /// `dstCapacity` is the original, uncompressed size to regenerate, returned by calling FL2_findDecompressedSize().
        /// Call FL2_decompressMt() to use > 1 thread. Specify nbThreads = 0 to use all cores. 
        /// The stream must contain dictionary resets to use multiple threads.These are inserted during compression by  default.
        /// The frequency can be changed/disabled with the FL2_p_resetInterval parameter setting.
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="dstCapacity"></param>
        /// <param name="src"></param>
        /// <param name="compressedSize"></param>
        /// <param name="nbThreads"></param>
        /// <returns>
        /// the number of bytes decompressed into `dst`, or an errorCode if it fails.
        /// </returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_decompressMt([In] byte[] dst, nuint dstCapacity, [In] byte[] src, nuint compressedSize, uint nbThreads);

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
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_findDecompressedSize([In] byte[] src, nuint srcSize);

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
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_findDecompressedSize(byte* src, nuint srcSize);

        /// <summary>
        /// Get the dictionary size property.
        /// Intended for use with the FL2_p_omitProperties parameter for creating a 7-zip or XZ compatible LZMA2 stream.
        /// </summary>
        /// <param name="prop"></param>
        /// <returns>Error code</returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_getDictSizeFromProp(byte prop);

        /// <summary>
        /// maximum compressed size in worst case scenario
        /// </summary>
        /// <param name="srcSize"></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_compressBound(nuint srcSize);

        /// <summary>
        /// maximum compression level available
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial int FL2_maxCLevel();

        /// <summary>
        /// maximum compression level available in high mode
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial int FL2_maxHighCLevel();

        /// <summary>
        /// Get all compression parameter values defined by the preset compressionLevel.
        /// </summary>
        /// <param name="compressionLevel"></param>
        /// <param name="high"></param>
        /// <param name="parameters"></param>
        /// <returns>
        /// the values in a FL2_compressionParameters struct, or the parameter_outOfBound error code
        /// (which can be tested with FL2_isError()) if compressionLevel is invalid.
        /// </returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_getLevelParameters(int compressionLevel, int high, ref CompressionParameters parameters);


        // FL2_estimate*() :
        // These functions estimate memory usage of a CCtx before its creation or before any operation has begun.
        // FL2_estimateCCtxSize() will provide a budget large enough for any compression level up to selected one.
        // To use FL2_estimateCCtxSize_usingCCtx, set the compression level and any other settings for the context,
        // then call the function. Some allocation occurs when the context is created, but the large memory buffers
        // used for string matching are allocated only when compression is initialized. 

        /// <summary>
        /// memory usage determined by level
        /// </summary>
        /// <param name="compressionLevel"></param>
        /// <param name="nbThreads"></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_estimateCCtxSize(int compressionLevel, uint nbThreads);

        /// <summary>
        /// memory usage determined by params
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="nbThreads"></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_estimateCCtxSize_byParams(ref CompressionParameters parameters, uint nbThreads);

        /// <summary>
        /// memory usage determined by settings
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_estimateCCtxSize_usingCCtx(IntPtr context);

        /// <summary>
        /// The size of a DCtx does not include a dictionary buffer because the caller must supply one.
        /// </summary>
        /// <param name="nbThreads"></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_estimateDCtxSize(uint nbThreads);

        #endregion Helper Functions

        #region Compress Context
        // Compression context
        // When compressing many times, it is recommended to allocate a context just once,
        //  and re-use it for each successive compression operation. This will make workload
        //  friendlier for system's memory. The context may not use the number of threads requested
        //  if the library is compiled for single-threaded compression or nbThreads > FL2_MAXTHREADS.
        //  Call FL2_getCCtxThreadCount to obtain the actual number allocated. 

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nint FL2_createCCtx();

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nint FL2_createCCtxMt(uint nbThreads);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nint FL2_freeCCtx(nint context);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial uint FL2_getCCtxThreadCount(nint context);

        /// <summary>
        /// Same as FL2_compress(), but requires an allocated FL2_CCtx (see FL2_createCCtx()).
        /// </summary>
        /// <param name="context"></param>
        /// <param name="dst"></param>
        /// <param name="dstCapacity"></param>
        /// <param name="src"></param>
        /// <param name="srcSize"></param>
        /// <param name="compressionLevel"></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_compressCCtx(nint context,
                                                      [In] byte[] dst, nuint dstCapacity,
                                                      [In] byte[] src, nuint srcSize,
                                                      int compressionLevel);

        /// <summary>
        /// Same as FL2_compress(), but requires an allocated FL2_CCtx (see FL2_createCCtx()).
        /// </summary>
        /// <param name="context"></param>
        /// <param name="dst"></param>
        /// <param name="dstCapacity"></param>
        /// <param name="src"></param>
        /// <param name="srcSize"></param>
        /// <param name="compressionLevel"></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_compressCCtx(nint context,
                                              byte* dst, nuint dstCapacity,
                                              byte* src, nuint srcSize,
                                              int compressionLevel);

        /// <summary>
        /// Get the dictionary size property.Intended for use with the FL2_p_omitProperties parameter for creating a 7-zip or XZ compatible LZMA2 stream.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial byte FL2_getCCtxDictProp(nint context);

        /// <summary>
        /// Set one compression parameter, selected by enum FL2_cParameter.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="param"></param>
        /// <param name="value"></param>
        /// <returns>
        /// informational value (typically, the one being set, possibly corrected),or an error code (which can be tested with FL2_isError()).
        /// </returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_CCtx_setParameter(nint context, FL2Parameter param, nuint value);

        /// <summary>
        /// Get one compression parameter, selected by enum FL2_cParameter.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="param"></param>
        /// <returns>
        /// the parameter value, or the parameter_unsupported error code (which can be tested with FL2_isError()).
        /// </returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_CCtx_getParameter(nint context, FL2Parameter param);

        #endregion Compress Context

        #region Decompress Context
        // Decompression context
        //  When decompressing many times, it is recommended to allocate a context only once,
        //  and re-use it for each successive decompression operation. This will make the workload
        //  friendlier for the system's memory.
        //  The context may not allocate the number of threads requested if the library is
        //  compiled for single-threaded compression or nbThreads > FL2_MAXTHREADS.
        //  Call FL2_getDCtxThreadCount to obtain the actual number allocated.
        //  At least nbThreads dictionary resets must exist in the stream to use all of the
        //  threads. Dictionary resets are inserted into the stream according to the
        //  FL2_p_resetInterval parameter used in the compression context. 

        
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nint FL2_createDCtx();

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nint FL2_createDCtxMt(uint nbThreads);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nint FL2_freeDCtx(nint context);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial uint FL2_getDCtxThreadCount(nint context);

        /// <summary>
        /// Use only when a property byte is not present at input byte 0. No init is necessary otherwise. The caller must store the result from FL2_getCCtxDictProp() and pass it to this function.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="prop"></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_initDCtx(nint context, byte prop);

        /// <summary>
        /// Same as FL2_decompress(), requires an allocated FL2_DCtx (see FL2_createDCtx()) 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="dst"></param>
        /// <param name="dstCapacity"></param>
        /// <param name="src"></param>
        /// <param name="srcSize"></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_decompressDCtx(nint context,
                                                        [In] byte[] dst, nuint dstCapacity,
                                                        [In] byte[] src, nuint srcSize);

        /// <summary>
        /// Same as FL2_decompress(), requires an allocated FL2_DCtx (see FL2_createDCtx()) 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="dst"></param>
        /// <param name="dstCapacity"></param>
        /// <param name="src"></param>
        /// <param name="srcSize"></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_decompressDCtx(nint context,
                                                byte* dst, nuint dstCapacity,
                                                byte* src, nuint srcSize);

        #endregion Decompress Context

        #region Compress Stream
        //  Streaming compression
     //
     //  A FL2_CStream object is required to track streaming operation.
     //  Use FL2_createCStream() and FL2_freeCStream() to create/release resources.
     //  FL2_CStream objects can be reused multiple times on consecutive compression operations.
     // It is recommended to re-use FL2_CStream in situations where many streaming operations will be done
     //  consecutively, since it will reduce allocation and initialization time.
     //
     //  Call FL2_createCStreamMt() with a nonzero dualBuffer parameter to use two input dictionary buffers.
     //  The stream will not block on FL2_compressStream() and continues to accept data while compression is
     //  underway, until both buffers are full.Useful when I/O is slow.
     // To compress with a single thread with dual buffering, call FL2_createCStreamMt with nbThreads = 1.
     //
     //Use FL2_initCStream() on the FL2_CStream object to start a new compression operation.
     //
     //  Use FL2_compressStream() repetitively to consume input stream.
     //  The function will automatically update the `pos` field.
     // It will always consume the entire input unless an error occurs or the dictionary buffer is filled,
     // unlike the decompression function.
     //
     // The radix match finder allows compressed data to be stored in its match table during encoding.
     // Applications may call streaming compression functions with output == NULL.In this case,
     // when the function returns 1, the compressed data must be read from the internal buffers.
     // Call FL2_getNextCompressedBuffer() repeatedly until it returns 0.
     //  Each call returns buffer information in the FL2_inBuffer parameter.Applications typically will
     // passed this to an I/O write function or downstream filter.
     //  Alternately, applications may pass an FL2_outBuffer object pointer to receive the output. In this
     //  case the return value is 1 if the buffer is full and more compressed data remains.
     //
     //  FL2_endStream() instructs to finish a stream. It will perform a flush and write the LZMA2
     // termination byte (required). Call FL2_endStream() repeatedly until it returns 0.
     //
     //  Most functions may return a size_t error code, which can be tested using FL2_isError().


        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nint FL2_createCStream();

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nint FL2_createCStreamMt(uint nbThreads, int dualBuffer);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial void FL2_freeCStream(nint fcs);

        /// <summary>
        /// Call this function before beginning a new compressed data stream. 
        /// To keep the stream object's  current parameters, specify zero for the compression level.
        /// The object is set to the default level upon creation.
        /// </summary>
        /// <param name="fcs"></param>
        /// <param name="compressionLevel"></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_initCStream(nint fcs, int compressionLevel);

        /// <summary>
        /// Sets a timeout in milliseconds. Zero disables the timeout (default). 
        /// If a nonzero timout is set, functions FL2_compressStream(), FL2_getDictionaryBuffer(), FL2_updateDictionary(), FL2_getNextCompressedBuffer(),
        /// FL2_flushStream(), and FL2_endStream() may return a timeout code before compression of the current dictionary of data completes.
        /// FL2_isError() returns true for the timeout code, so check the code with FL2_isTimedOut() before testing for errors.
        /// With the exception of FL2_updateDictionary(), the above functions may be called again to wait for completion.
        /// A typical application for timeouts is to update the user on compression progress.
        /// </summary>
        /// <param name="fcs"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_setCStreamTimeout(nint fcs, uint timeout);

        /// <summary>
        /// Reads data from input into the dictionary buffer. Compression will begin if the buffer fills up.
        /// A dual buffering stream will fill the second buffer while compression proceeds on the first.
        /// A call to FL2_compressStream() will wait for ongoing compression to complete if all dictionary space is filled.
        ///  FL2_compressStream() must not be called with output == NULL unless the caller has read all compressed data from the CStream object.
        /// </summary>
        /// <param name="fcs"></param>
        /// <param name="output"></param>
        /// <param name="input"></param>
        /// <returns>1 to indicate compressed data must be read (or output is full), or 0 otherwise.</returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_compressStream(nint fcs, ref FL2OutBuffer output, ref FL2InBuffer input);

        /// <summary>
        /// Copies compressed data to the output buffer until the buffer is full or all available data is copied.
        /// If asynchronous compression is in progress, the function returns 0 without waiting.
        /// </summary>
        /// <param name="fcs"></param>
        /// <param name="output"></param>
        /// <returns>1 to indicate some compressed data remains, or 0 otherwise.</returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_copyCStreamOutput(nint fcs, ref FL2OutBuffer output);

        /// <summary>
        /// Returns a buffer in the FL2_outBuffer object, which the caller can directly read data into.
        /// Applications will normally pass this buffer to an I/O read function or upstream filter.
        /// </summary>
        /// <param name="fcs"></param>
        /// <param name="dict"></param>
        /// <returns>0, or an error or timeout code.</returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_getDictionaryBuffer(nint fcs, ref FL2DictBuffer dict);

        /// <summary>
        /// Informs the CStream how much data was added to the buffer. Compression begins if the dictionary was filled.
        /// </summary>
        /// <param name="fcs"></param>
        /// <param name="addedSize"></param>
        /// <returns>1 to indicate compressed data must be read, 0 if not, or an error code.</returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_updateDictionary(nint fcs, nuint addedSize);

        /// <summary>
        /// Returns a buffer containing a slice of the compressed data. Call this function and process the data until the function returns zero.
        ///  In most cases it will return a buffer for each compression thread used.
        /// It is sometimes less but never more than nbThreads.If asynchronous compression is in progress,
        ///  this function will wait for completion before returning, or it will return the timeout code.
        /// </summary>
        /// <param name="fcs"></param>
        /// <param name="cbuf"></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_getNextCompressedBuffer(nint fcs, ref FL2cBuffer cbuf);
        /// <summary>
        /// Returns the number of bytes processed since the stream was initialized. 
        /// This is a synthetic estimate because the match finder does not proceed sequentially through the data.
        /// If outputSize is not NULL, returns the number of bytes of compressed data generated.
        /// </summary>
        /// <param name="fcs"></param>
        /// <param name="outputSize"></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_getCStreamProgress(nint fcs, ulong outputSize);

        /// <summary>
        /// Waits for compression to end. 
        /// This function returns after the timeout set using FL2_setCStreamTimeout has elapsed.
        /// Unnecessary when no timeout is set.
        /// </summary>
        /// <param name="fcs"></param>
        /// <returns>1 if compressed output is available, 0 if not, or the timeout code.</returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_waitCStream(nint fcs);

        /// <summary>
        /// Cancels any compression operation underway. 
        /// Useful only when dual buffering and/or timeouts  are enabled.
        /// The stream will be returned to an uninitialized state.
        /// </summary>
        /// <param name="fcs"></param>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial void FL2_cancelCStream(nint fcs);
        /// <summary>
        /// The amount of compressed data remaining to be read from the CStream object.
        /// </summary>
        /// <param name="fcs"></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_remainingOutputSize(nint fcs);

        /// <summary>
        /// Compress all data remaining in the dictionary buffer(s). It may be necessary to call FL2_flushStream() more than once.
        /// If output == NULL the compressed data must be read from the CStream object after each call.
        /// Flushing is not normally useful and produces larger output.
        /// </summary>
        /// <param name="fcs"></param>
        /// <param name="output"></param>
        /// <returns>1 if input or output still exists in the CStream object, 0 if complete, or an error code.</returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_flushStream(nint fcs, ref FL2OutBuffer output);

        /// <summary>
        /// Compress all data remaining in the dictionary buffer(s) and write the stream end marker. 
        /// It may be necessary to call FL2_endStream() more than once.
        /// If output == NULL the compressed data must be read from the CStream object after each call.
        /// </summary>
        /// <param name="fcs"></param>
        /// <param name="output"></param>
        /// <returns>0 when compression is complete and all output has been flushed, 1 if not complete, or an error code.</returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_endStream(nint fcs, ref FL2OutBuffer output);

        /// <summary>
        /// Set one compression parameter, selected by enum FL2_cParameter.
        /// </summary>
        /// <param name="fcs"></param>
        /// <param name="param"></param>
        /// <param name="value"></param>
        /// <returns>
        /// informational value (typically, the one being set, possibly corrected),or an error code (which can be tested with FL2_isError()).
        /// </returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_CStream_setParameter(nint fcs, FL2Parameter param, nuint value);

        /// <summary>
        /// Get one compression parameter, selected by enum FL2_cParameter.
        /// </summary>
        /// <param name="fcs"></param>
        /// <param name="param"></param>
        /// <returns>
        /// the parameter value, or the parameter_unsupported error code (which can be tested with FL2_isError()).
        /// </returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_CStream_getParameter(nint fcs, FL2Parameter param);
        /// <summary>
        /// memory usage determined by level
        /// </summary>
        /// <param name="compressionLevel"></param>
        /// <param name="nbThreads"></param>
        /// <param name="dualBuffer"></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_estimateCStreamSize(int compressionLevel, uint nbThreads, int dualBuffer);
        /// <summary>
        /// memory usage determined by params
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="nbThreads"></param>
        /// <param name="dualBuffer"></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_estimateCStreamSize_byParams(ref CompressionParameters parameters, uint nbThreads, int dualBuffer);

        /// <summary>
        /// memory usage determined by settings
        /// </summary>
        /// <param name="fcs"></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_estimateCStreamSize_usingCStream(nint fcs);

        #endregion Compress Stream

        #region Decompress Stream
        //  Streaming decompression
        //
        //  A FL2_DStream object is required to track streaming operations.
        //  Use FL2_createDStream() and FL2_freeDStream() to create/release resources.
        //  FL2_DStream objects can be re-used multiple times.
        //
        // Use FL2_initDStream() to start a new decompression operation.
        //  @return : zero or an error code
        //
        // Use FL2_decompressStream() repetitively to consume your input.
        //  The function will update both `pos` fields.
        // If `input.pos<input.size`, some input has not been consumed.
        //  It's up to the caller to present again the remaining data.
        //  If `output.pos<output.size`, decoder has flushed everything it could.
        //  @return : 0 when a stream is completely decoded and fully flushed,
        //            1, which means there is still some decoding to do to complete the stream,
        //            or an error code, which can be tested using FL2_isError().


        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nint FL2_createDStream();

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nint FL2_createDStreamMt(uint nbThreads);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_freeDStream(nint fds);

        /// <summary>
        /// Set a total size limit for multithreaded decoder input and output buffers.  MT decoder memory
        /// usage is unknown until the input is parsed.If the limit is exceeded, the decoder switches to using a single thread.
        ///  MT decoding memory usage is typically dictionary_size * 4 * nbThreads for the output
        ///  buffers plus the size of the compressed input for that amount of output.
        /// </summary>
        /// <param name="fds"></param>
        /// <param name="limit"></param>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial void FL2_setDStreamMemoryLimitMt(nint fds, nuint limit);

        /// <summary>
        /// Sets a timeout in milliseconds. Zero disables the timeout. 
        /// If a nonzero timout is set,FL2_decompressStream() may return a timeout code before decompression of the available data completes.
        /// FL2_isError() returns true for the timeout code, so check the code with FL2_isTimedOut() before testing for errors.
        /// After a timeout occurs, do not call FL2_decompressStream() again unless a call to FL2_waitDStream() returns 1. 
        /// A typical application for timeouts is to update the user on decompression progress.
        /// </summary>
        /// <param name="fds"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_setDStreamTimeout(nint fds, uint timeout);

        /// <summary>
        /// Waits for decompression to end after a timeout has occurred. 
        /// This function returns after the timeout set using FL2_setDStreamTimeout() has elapsed, 
        /// or when decompression of available input is complete.Unnecessary when no timeout is set.
        /// </summary>
        /// <param name="fds"></param>
        /// <returns>0 if the stream is complete, 1 if not complete, or an error code.</returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_waitDStream(nint fds);

        /// <summary>
        /// Frees memory allocated for MT decoding. 
        /// If a timeout is set and the caller is waiting for completion of MT decoding, 
        /// decompression in progress will be canceled.
        /// </summary>
        /// <param name="fds"></param>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial void FL2_cancelDStream(nint fds);

        /// <summary>
        /// Returns the number of bytes decoded since the stream was initialized.
        /// </summary>
        /// <param name="fds"></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial ulong FL2_getDStreamProgress(nint fds);

        /// <summary>
        /// Call this function before decompressing a stream. 
        /// FL2_initDStream_withProp()  must be used for streams which do not include a property byte at position zero.
     ///The caller is responsible for storing and passing the property byte.
        /// </summary>
        /// <param name="fds"></param>
        /// <returns>0 if okay, or an error if the stream object is still in use from a previous call to FL2_decompressStream() (see timeout info above).</returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_initDStream(nint fds);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_initDStream_withProp(nint fds, byte prop);

        /// <summary>
        /// Reads data from input and decompresses to output.
        /// Returns 1 if the stream is unfinished, 0 if the terminator was encountered(he'll be back)
        /// and all data was written to output, or an error code. 
        /// Call this function repeatedly if necessary, removing data from output and/or loading data into input before each call.
        /// </summary>
        /// <param name="fds"></param>
        /// <param name="output"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_decompressStream(nint fds, ref FL2OutBuffer output, ref FL2InBuffer input);

        /// <summary>
        /// Estimate decompression memory use from the dictionary size and number of threads.
        ///For nbThreads == 0 the number of available cores will be used.
        ///Obtain dictSize by passing the property byte to FL2_getDictSizeFromProp.
        /// </summary>
        /// <param name="dictSize"></param>
        /// <param name="nbThreads"></param>
        /// <returns></returns>
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial nuint FL2_estimateDStreamSize(nuint dictSize, uint nbThreads);

        #endregion Decompress Stream
    }
}