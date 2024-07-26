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

            FileInfo srcFile = new FileInfo(@"d:\NatTypeTester.exe");
            FileInfo dstFile = new FileInfo(@"d:\NatTypeTester.fl2");
            //compressor.Compress(@"d:\半条命1三合一.tar", @"d:\半条命1三合一.tar.fl2");
            compressor.CompressLevel = FL2.CompressionLevelMax;
            byte[] compressed = compressor.Compress(File.ReadAllBytes(@"D:\工作流测试请求.bin"));

            using (var fs = File.OpenWrite(@"d:\NatTypeTester.fl2"))
            {
                fs.Write(compressed, 0, compressed.Length);
            }
            //byte[] compressed = File.ReadAllBytes(dstFile.FullName);

            //byte[] recovery = FL2.DecompressMT(compressed,0);
            byte[] origin = File.ReadAllBytes(@"D:\工作流测试请求.bin");

            //byte[] hash1 = SHA256.HashData(recovery);
            //byte[] hash2 = SHA256.HashData(origin);

            //var cmp = hash1.SequenceEqual(hash2);
            //using (FileStream recoveryFile = File.OpenWrite("D:\\recovery.tar"))
            //{
            //    recoveryFile.Write(recovery);
            //}
            nint streamCtx = ExternMethods.FL2_createDStream();


            nuint code = ExternMethods.FL2_initDStream(streamCtx);

            byte[] buffer = new byte[81920];
            using (MemoryStream recoveryStream = new MemoryStream())
            {
                using (MemoryStream ms = new MemoryStream(compressed))
                {
                    using (DecompressionStream ds = new DecompressionStream(ms))
                    {
                        int reads = 0;
                        while ((reads = ds.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            PrintHex(buffer[0..reads]);
                            recoveryStream.Write(buffer, 0, reads);
                        }
                    }
                }
                byte[] dsResult = recoveryStream.ToArray();
                var same = origin.SequenceEqual(dsResult);
            }
            
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