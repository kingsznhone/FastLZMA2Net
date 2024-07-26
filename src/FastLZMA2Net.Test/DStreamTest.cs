using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FastLZMA2Net;

namespace Test
{
    [TestClass]
    public class DStreamTest
    {
        [TestMethod]
        public void TestSimpleDStream()
        {
            //Use Dummy File
            byte[] origin = File.ReadAllBytes(@"Resources/dummy.raw");
            byte[] compressed = FL2.CompressMT(origin, 1,0);
            //// Use Random byte[]
            //byte[] origin = new byte[4096 * 1024];
            //new Random().NextBytes(origin);
            //byte[] compressed = FL2.Compress(origin, 10);

            //Test Block buffer
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
                            recoveryStream.Write(buffer, 0, reads);
                        }
                    }
                }
                byte[] dsResult = recoveryStream.ToArray();
                Assert.IsTrue(origin.SequenceEqual(dsResult));
            }
        }

        [TestMethod]
        public void TestStreamingDStream()
        {
            //Use Dummy file
            byte[] origin = File.ReadAllBytes(@"Resources/dummy.raw");
            byte[] compressed = FL2.CompressMT(origin, 1, 0);

            //// Use Random byte[]
            //byte[] origin = new byte[4096 * 1024];
            //new Random().NextBytes(origin);
            //byte[] compressed = FL2.Compress(origin, 10);

            //test direct file access
            using (FileStream recoveryStream = new FileStream(@"Resources/dummy.recovery",FileMode.OpenOrCreate,FileAccess.ReadWrite))
            {
                using (FileStream fs = new FileStream(@"Resources/dummy.fl2", FileMode.Open, FileAccess.Read))
                {
                    using (DecompressionStream ds = new DecompressionStream(fs))
                    {
                        ds.CopyTo(recoveryStream);
                    }
                }
            }
            Assert.IsTrue(origin.SequenceEqual(File.ReadAllBytes(@"Resources/dummy.recovery")));
        }
    }
}
