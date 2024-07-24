using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FastLZMA2Net
{
    public partial class Decompressor : IDisposable
    {
        const string LibraryName = "fast-lzma2";

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial nint FL2_createDCtx();

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial nint FL2_createDCtxMt(uint nbThreads);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial nint FL2_freeDCtx(nint context);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial uint FL2_getDCtxThreadCount(nint context);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial nuint FL2_initDCtx(nint context, byte prop);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial nuint FL2_decompressDCtx(nint context,
                                                        byte[] dst, nuint dstCapacity,
                                                        byte[] src, nuint srcSize);

        private nint _context;
        public uint ThreadCount => FL2_getDCtxThreadCount(_context);

        public Decompressor()
        {
            _context = FL2_createDCtx();
        }

        public Decompressor(uint nThread)
        {
            _context = FL2_createDCtxMt(nThread);
        }

        public void Dispose()
        {
            FL2_freeDCtx(_context);
        }

        public byte[] Decompress(byte[] data)
        {
            nuint decompressedSize = FL2.FindDecompressedSize(data);
            byte[] decompressed = new byte[decompressedSize];
            nuint code = FL2_decompressDCtx(_context, decompressed, decompressedSize, data, (nuint)data.Length);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return decompressed;
        }
    }
}