using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FastLZMA2Net;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Test
{
    [TestClass]
    public class CStreamTest
    {
        public CStreamTest()
        {
            File.WriteAllBytes(@"Resources/dummy.fl2", FL2.CompressMT(File.ReadAllBytes(@"Resources/dummy.raw"), 9, 0));
        }

        [TestMethod]
        public void TestBlocked()
        {
            byte[] origin = File.ReadAllBytes(@"Resources/dummy.raw");
            byte[] compressedRef = File.ReadAllBytes(@"Resources/dummy.fl2");

            using (MemoryStream resultStream = new MemoryStream())
            {
                using (CompressionStream ds = new CompressionStream(resultStream))
                {
                    int offset = 0;
                    while (offset < origin.Length)
                    {
                        int remaining = origin.Length - offset;
                        int bytesToWrite = Math.Min(1024, remaining);
                        ds.Append(origin, offset, bytesToWrite);
                        offset += bytesToWrite;
                    }
                    ds.Flush();
                }
                byte[] compressed = resultStream.ToArray();
                byte[] recovery = FL2.Decompress(compressed);
                Debug.Assert(origin.SequenceEqual(recovery));
            }
        }

        [TestMethod]
        public void TestOnetime()
        {
            byte[] origin = File.ReadAllBytes(@"Resources/dummy.raw");
            byte[] compressedRef = File.ReadAllBytes(@"Resources/dummy.fl2");

            using (MemoryStream resultStream = new MemoryStream())
            {
                using (CompressionStream ds = new CompressionStream(resultStream))
                {
                    ds.Write(origin);
                }
                byte[] compressed = resultStream.ToArray();
                byte[] recovery = FL2.Decompress(compressed);
                Debug.Assert(origin.SequenceEqual(recovery));
            }
        }
        [TestMethod]
        public async Task TestAsync()
        {
            byte[] origin = File.ReadAllBytes(@"Resources/dummy.raw");
            byte[] compressedRef = File.ReadAllBytes(@"Resources/dummy.fl2");

            using (MemoryStream resultStream = new MemoryStream())
            {
                using (CompressionStream ds = new CompressionStream(resultStream))
                {
                    await ds.WriteAsync(origin);
                }
                byte[] compressed = resultStream.ToArray();
                byte[] recovery = FL2.Decompress(compressed);
                Debug.Assert(origin.SequenceEqual(recovery));
            }
        }
    }
}
