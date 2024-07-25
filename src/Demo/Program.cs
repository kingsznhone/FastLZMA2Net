using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using FastLZMA2Net;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Demo
{
    internal unsafe class Program
    {
        private static void Main(string[] args)
        {
            int size = IntPtr.Size;
            ExternMethods.SetWinDllDirectory();
            byte[] src = new byte[128*1048576];
            for (int i = 0; i < src.Length; i++)
            {
                src[i] = (byte)(i % 0xf); // 使用模运算确保值在 0x0 到 0xf 之间
            }

            //new Random().NextBytes(src);
            Compressor compressor = new();

            var dictsize = compressor.DictionarySize;
            //byte[] compressed = compressor.Compress(src);

            //using (var fs = File.OpenWrite(@"d:\temp.lz2"))
            //{
            //    fs.Write(compressed, 0, compressed.Length);
            //}
            byte[] compressed = File.ReadAllBytes(@"d:\temp.lz2");
            nint streamCtx = ExternMethods.FL2_createDStream();
            nuint code = ExternMethods.FL2_initDStream(streamCtx);

            MemoryStream ms = new MemoryStream();

            GCHandle srcHandle = GCHandle.Alloc(compressed, GCHandleType.Pinned);
            FL2InBuffer inBuffer = new FL2InBuffer()
            {
                src = srcHandle.AddrOfPinnedObject(),
                size =(nuint) compressed.Length,
                pos = 0
            };
            byte[] dstArray = new byte[1048576];
            GCHandle dstHandle = GCHandle.Alloc(dstArray, GCHandleType.Pinned);
            FL2OutBuffer outBuffer = new FL2OutBuffer()
            {
                dst = dstHandle.AddrOfPinnedObject(),
                size =(nuint) dstArray.Length,
                pos = 0
            };
            FileInfo localFile = new FileInfo(@"d:\temp.lz2");
            
            // 获取内存映射视图
            var unzipLength =FL2.FindDecompressedSize(localFile.FullName);

            code = ExternMethods.FL2_decompressStream(streamCtx,ref outBuffer,ref inBuffer);
            ms.Write(dstArray.AsSpan());

            outBuffer.pos = 0;
            code = ExternMethods.FL2_decompressStream(streamCtx, ref outBuffer, ref inBuffer);
            ms.Write(dstArray.AsSpan());

            var progress  = ExternMethods.FL2_getDStreamProgress(streamCtx);

            outBuffer.pos = 0;
            code = ExternMethods.FL2_decompressStream(streamCtx, ref outBuffer, ref inBuffer);
            ms.Write(dstArray.AsSpan());

            progress = ExternMethods.FL2_getDStreamProgress(streamCtx);

            outBuffer.pos = 0;
            code = ExternMethods.FL2_decompressStream(streamCtx, ref outBuffer, ref inBuffer);
            ms.Write(dstArray.AsSpan());

            outBuffer.pos = 0;
            code = ExternMethods.FL2_decompressStream(streamCtx, ref outBuffer, ref inBuffer);
            byte[] decompressed = ms.ToArray();

            var debug = src.SequenceEqual(decompressed);

            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            Console.WriteLine(code);
        }

        private static void PrintHex(byte[] byteArray)
        {
            foreach (byte b in byteArray)
            {
                Console.Write($"{b:X2} ");
            }
            Console.WriteLine(Environment.NewLine);
        }
    }
}