using System;
using System.Diagnostics;
using System.IO.Compression;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using FastLZMA2Net;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Demo
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            int size = IntPtr.Size;
            NativeMethods.SetWinDllDirectory();
            byte[] src = new byte[128*1048576];
            for (int i = 0; i < src.Length; i++)
            {
                src[i] = (byte)(i % 0xf); // 使用模运算确保值在 0x0 到 0xf 之间
            }
            
            Compressor compressor = new(0) { CompressLevel = 10,HighCompressLevel=1};
            Console.WriteLine( compressor.DictSizeProperty);
            FileInfo srcFile = new FileInfo(@"d:\ffmpeg.exe");
            FileInfo dstFile = new FileInfo(@"d:\半条命1三合一.fl2");
            //compressor.Compress(@"d:\半条命1三合一.tar", @"d:\半条命1三合一.fl2");
            byte[] origin = File.ReadAllBytes(@"D:\半条命1三合一.tar");
            byte[] compressedRef = File.ReadAllBytes(@"D:\半条命1三合一.fl2");
            
            using FileStream sourceFile = File.OpenRead(@"D:\Devotion v1.05.tar");
            using FileStream compressedFile = File.OpenRead(@"D:\Devotion v1.05.tar.fl2");
            using FileStream recoveryFile = File.OpenWrite(@"D:\recovery.tar");

            var length = recoveryFile.Length;
            byte[] buffer = new byte[256 * 1024 * 1024];
            //using (DecompressionStream ds = new DecompressionStream(compressedFile))
            //{
            //    ds.CopyTo(recoveryFile);
            //}

            return;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            using (CompressionStream cs = new CompressionStream(compressedFile, outBufferSize: 64 * 1024 * 1024, nbThreads: 0))
            {
                var clevel = cs.GetParameter(CompressParameterEnum.FL2_p_compressionLevel);
                long offset = 0;
                while (offset < sourceFile.Length)
                {
                    long remaining = sourceFile.Length - offset;
                    int bytesToWrite =(int) Math.Min(64 * 1024 * 1024, remaining);
                    sourceFile.Read(buffer, 0, bytesToWrite);
                    cs.Append(buffer,0, bytesToWrite);
                    offset += bytesToWrite;
                }
                cs.Flush();
            }

            sw.Stop();
           
            sourceFile.Close();
            compressedFile.Close();
            Console.WriteLine($"{sw.Elapsed.TotalSeconds}s");
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