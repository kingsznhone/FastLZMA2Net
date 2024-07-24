using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FastLZMA2Net
{
    //FL2CompressContext
    public partial class Compressor : IDisposable
    {
        const string LibraryName = "fast-lzma2";
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial nint FL2_createCCtx();

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial nint FL2_createCCtxMt(uint nbThreads);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial nint FL2_freeCCtx(nint context);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial uint FL2_getCCtxThreadCount(nint context);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial nuint FL2_compressCCtx(nint context,
                                                      byte[] dst, nuint dstCapacity,
                                                      byte[] src, nuint srcSize,
                                                      int compressionLevel);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial byte FL2_getCCtxDictProp(nint context);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial nuint FL2_CCtx_setParameter(nint context, CompressParameterEnum param, nuint value);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial nuint FL2_CCtx_getParameter(nint context, CompressParameterEnum param);

        private nint _context;
        public uint ThreadCount => FL2_getCCtxThreadCount(_context);
        public byte DictSizeProperty => FL2_getCCtxDictProp(_context);

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


        public Compressor()
        {
            _context = FL2_createCCtx();
        }

        public Compressor(uint nThread)
        {
            _context = FL2_createCCtxMt(nThread);
        }

        public void Dispose()
        {
            FL2_freeCCtx(_context);
        }

        public byte[] Compress(byte[] data)
        {
            return Compress(data, CompressLevel);
        }

        public byte[] Compress(byte[] data, int Level)
        {
            byte[] compressed = new byte[FL2.CompressBound(data)];
            nuint code = FL2_compressCCtx(_context, compressed, (nuint)compressed.Length, data, (nuint)data.Length, Level);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return compressed[..(int)code];
        }

        public void SetParameter(CompressParameterEnum param, nuint value)
        {
            var code = FL2_CCtx_setParameter(_context, param, value);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
        }

        public nuint GetParameter(CompressParameterEnum param)
        {
            var code = FL2_CCtx_getParameter(_context, param);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return code;
        }
    }
}