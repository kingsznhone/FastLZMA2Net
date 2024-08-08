using System.Diagnostics;
using FastLZMA2Net;

namespace Demo
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            string SourceFilePath = @"D:\dummy.tar";
            string CompressedFilePath = @"D:\dummy.tar.fl2";
            string DecompressedFilePath = @"D:\dummy.recovery.tar";
            // Simple compression
            byte[] origin = File.ReadAllBytes(SourceFilePath);
            byte[] compressed = FL2.Compress(origin,0);
            byte[] decompressed = FL2.Decompress(compressed);

            // Context compression, context can be reuse.
            Compressor compressor = new(0) { CompressLevel = 10 };
            compressed = compressor.Compress(origin);
            compressed = compressor.Compress(origin);
            compressed = compressor.Compress(origin);

            Decompressor decompressor = new Decompressor();
            decompressed = decompressor.Decompress(compressed);
            decompressed = decompressor.Decompress(compressed);
            decompressed = decompressor.Decompress(compressed);

            // Streaming Compression 
            byte[] buffer = new byte[256 * 1024 * 1024]; 
            // use 256MB input buffer
            
            // small file or data (<2GB)
            using (MemoryStream ms = new MemoryStream())
            {
                using (CompressStream cs = new CompressStream(ms))
                {
                    cs.Write(origin);
                }
                compressed = ms.ToArray();
            }

            //large file streaming compression using Direct file access(>2GB)
            using (FileStream compressedFile = File.OpenWrite(CompressedFilePath))
            {
                using (CompressStream cs = new CompressStream(compressedFile))
                {
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
                        // make sure always use Flush() after Append()
                        //Flush() add checksum to stream and finish streaming.
                        cs.Flush();
                    }
                }
            }

            //large file streaming decompress(>2GB)
            using (FileStream recoveryStream = File.OpenWrite(DecompressedFilePath))
            {
                using (FileStream compressedFile = File.OpenRead(CompressedFilePath))
                {
                    using (DecompressStream ds = new DecompressStream(compressedFile))
                    {
                        ds.CopyTo(recoveryStream);
                    }
                }
            }

        }
    }
}