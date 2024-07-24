using System.Diagnostics;
using System.Reflection.Emit;
using FastLZMA2Net;

namespace Test
{
    [TestClass]
    public class FL2Test
    {

        [TestMethod]
        public void Simple()
        {
            byte[] src = new byte[4096*1024];
            new Random().NextBytes(src);

            byte[] compressed = FL2.Compress(src, 10);
            byte[] decompressed = FL2.Decompress(compressed);
            Assert.IsTrue(src.SequenceEqual(decompressed), "The byte arrays are not equal.");
        }

        [TestMethod]
        public void SimpleMT()
        {
            byte[] src = new byte[4096 * 1024];
            new Random().NextBytes(src);

            byte[] compressed = FL2.CompressMT(src, 10,0);
            byte[] decompressed = FL2.DecompressMT(compressed, 0);
            Assert.IsTrue(src.SequenceEqual(decompressed), "The byte arrays are not equal.");
        }
        [TestMethod]
        public void GetPresetParams()
        {
            CompressionParameters parameters;
            for (int level = 0; level <= 10; level++)
            {
                parameters = FL2.GetPresetLevelParameters(level, 0);
            }

            var exception = Assert.ThrowsException<FL2Exception>(() =>
            {
                parameters = FL2.GetPresetLevelParameters(-1, 0);
            });
            exception = Assert.ThrowsException<FL2Exception>(() =>
            {
                parameters = FL2.GetPresetLevelParameters(11, 0);
            });

            Console.WriteLine($"Max Level{FL2.MaxCompressionLevel}");
            Console.WriteLine($"Max High Level{FL2.MaxHighCompressionLevel}");
        }
    }
}