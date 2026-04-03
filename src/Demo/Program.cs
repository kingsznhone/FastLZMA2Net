using FastLZMA2Net;
using System.Diagnostics;

namespace Demo
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Console.WriteLine("FL2 Version: " + FL2.VersionString);
            string SourceFilePath = @"D:\dummy.tar";
            string CompressedFilePath = @"D:\dummy.tar.fl2";
            string DecompressedFilePath = @"D:\dummy.recovery.tar";

            // Simple compression
            byte[] origin = File.ReadAllBytes(SourceFilePath);
            byte[] compressed = FL2.Compress(origin, 0);
            byte[] decompressed = FL2.Decompress(compressed);

            // Context compression, context can be reuse.
            Compressor compressor = new(0) { CompressLevel = 10 };
            compressor.CompressLevel = 10;
            FL2.EstimateCompressMemoryUsage(compressor.CompressLevel, compressor.ThreadCount);
            Console.WriteLine($"Estimated memory usage for compression: {FL2.EstimateCompressMemoryUsage(compressor.CompressLevel, compressor.ThreadCount) / 1024.0 / 1024.0:F2} MB");
            compressed = compressor.Compress(origin);
            Console.WriteLine($"{compressed.Length} compressed");

            Decompressor decompressor = new Decompressor();
            decompressed = decompressor.Decompress(compressed);

            // small file or data (<2GB)
            using (MemoryStream ms = new MemoryStream())
            {
                using (CompressStream cs = new CompressStream(ms))
                {
                    cs.Write(origin);
                }
                compressed = ms.ToArray();
            }

            // Streaming Compression
            // use 256MB input buffer
            byte[] buffer = new byte[256 * 1024 * 1024];

            //large file streaming compression using Direct file access(>2GB)
            long sourceFileSize = new FileInfo(SourceFilePath).Length;
            Stopwatch swCompress = Stopwatch.StartNew();
            using (FileStream compressedFile = File.Create(CompressedFilePath))
            {
                using (CompressStream cs = new CompressStream(compressedFile))
                {
                    cs.CompressLevel = 10;
                    cs.HighCompressLevel = 10;
                    using (FileStream sourceFile = File.OpenRead(SourceFilePath))
                    {
                        //DO NOT USE sourceFile.CopyTo(cs)
                        // CopyTo calls write() internal, which terminate stream after 1 cycle.
                        long offset = 0;
                        while (offset < sourceFile.Length)
                        {
                            long remaining = sourceFile.Length - offset;
                            int bytesToWrite = (int)Math.Min(64 * 1024 * 1024, remaining);
                            sourceFile.Read(buffer, 0, bytesToWrite);
                            cs.Append(buffer, 0, bytesToWrite);
                            offset += bytesToWrite;
                        }
                        // Flush() is called automatically by Dispose()
                    }
                }
            }
            swCompress.Stop();
            double compressSeconds = swCompress.Elapsed.TotalSeconds;
            double compressSpeedMBps = sourceFileSize / 1024.0 / 1024.0 / compressSeconds;
            Console.WriteLine($"Streaming compression finished: {compressSeconds:F2}s, {compressSpeedMBps:F2} MB/s");

            //large file streaming decompress to file(>2GB)
            Stopwatch swDecompress = Stopwatch.StartNew();
            using (FileStream recoveryStream = File.Create(DecompressedFilePath))
            {
                using (FileStream compressedFile = File.OpenRead(CompressedFilePath))
                {
                    using (DecompressStream ds = new DecompressStream(compressedFile))
                    {
                        ds.CopyTo(recoveryStream);
                    }
                }
            }
            swDecompress.Stop();
            double decompressSeconds = swDecompress.Elapsed.TotalSeconds;
            double decompressSpeedMBps = sourceFileSize / 1024.0 / 1024.0 / decompressSeconds;
            Console.WriteLine($"Streaming decompression finished: {decompressSeconds:F2}s, {decompressSpeedMBps:F2} MB/s");
        }
    }
}