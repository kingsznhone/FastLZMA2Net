using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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
            Compressor compressor = new(0);

            var dictsize = compressor.DictionarySize;

            FileInfo srcFile = new FileInfo(@"d:\半条命1三合一.tar");
            FileInfo dstFile = new FileInfo(@"d:\半条命1三合一.tar.fl2");
            compressor.Compress(@"d:\半条命1三合一.tar", @"d:\半条命1三合一.tar.fl2");
            compressor.CompressLevel = FL2.CompressionLevelMax;
            //byte[] compressed = compressor.Compress(src);

            //using (var fs = File.OpenWrite(@"d:\temp.lz2"))
            //{
            //    fs.Write(compressed, 0, compressed.Length);
            //}
            byte[] compressed = File.ReadAllBytes(dstFile.FullName);

            byte[] recovery = FL2.Decompress(compressed);
            byte[] origin = File.ReadAllBytes(srcFile.FullName);

            byte[] hash1 = SHA256.HashData(recovery);
            byte[] hash2 = SHA256.HashData(origin);

            var cmp = hash1.SequenceEqual(hash2);
            nint streamCtx = ExternMethods.FL2_createDStream();

            using (FileStream recoveryFile = File.OpenWrite("D:\\recovery.tar"))
            {
                recoveryFile.Write(recovery);
            }
            nuint code = ExternMethods.FL2_initDStream(streamCtx);

            
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

            //var unzipLength =FL2.FindDecompressedSize(srcFile.FullName);
            var ms = new MemoryStream();
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