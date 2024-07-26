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
            byte[] origin = File.ReadAllBytes(@"D:\半条命1三合一.tar");
            byte[] compressed = FL2.CompressMT(origin, 1,0);
            //byte[] origin = new byte[4096 * 1024];
            //new Random().NextBytes(origin);
            //byte[] compressed = FL2.Compress(origin, 10);
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
            //test direct file access
            using (MemoryStream recoveryStream = new MemoryStream())
            {
                using (FileStream fs = new FileStream(@"D:\半条命1三合一.lz2",FileMode.Open,FileAccess.Read))
                {
                    using (DecompressionStream ds = new DecompressionStream(fs))
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
    }
}
