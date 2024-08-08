using System.Diagnostics;
using FastLZMA2Net;

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
                using (CompressStream ds = new CompressStream(resultStream))
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
                using (CompressStream ds = new CompressStream(resultStream))
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
                using (CompressStream ds = new CompressStream(resultStream))
                {
                    await ds.WriteAsync(origin);
                }
                byte[] compressed = resultStream.ToArray();
                byte[] recovery = FL2.Decompress(compressed);
                Debug.Assert(origin.SequenceEqual(recovery));
            }
        }

        [TestMethod]
        public void TestDFA()
        {
            byte[] buffer = new byte[1024];
            using FileStream sourceFile = File.OpenRead(@"Resources/dummy.raw");
            using FileStream compressedFile = File.OpenWrite(@"Resources/dummy.fl2");
            using (CompressStream cs = new CompressStream(compressedFile, outBufferSize: 1024, nbThreads: 0))
            {
                long offset = 0;
                while (offset < sourceFile.Length)
                {
                    long remaining = sourceFile.Length - offset;
                    int bytesToWrite = (int)Math.Min(1024, remaining);
                    sourceFile.Read(buffer, 0, bytesToWrite);
                    cs.Append(buffer, 0, bytesToWrite);
                    offset += bytesToWrite;
                }
                cs.Flush();
            }

            sourceFile.Close();
            compressedFile.Close();
            byte[] compressed = File.ReadAllBytes(@"Resources/dummy.fl2");
            byte[] recovery = FL2.Decompress(compressed);
            byte[] origin = File.ReadAllBytes(@"Resources/dummy.raw");
            Assert.IsTrue(origin.SequenceEqual(recovery));
        }
    }
}