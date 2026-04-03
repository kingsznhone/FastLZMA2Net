using FastLZMA2Net;

namespace Test
{
    [TestClass]
    public class SimpleTest
    {
        public SimpleTest()
        {
            File.WriteAllBytes(@"Resources/dummy.fl2", FL2.CompressMT(File.ReadAllBytes(@"Resources/dummy.raw"), 9, 0));
        }

        [TestMethod]
        public void Simple()
        {
            byte[] origin = File.ReadAllBytes(@"Resources/dummy.raw");

            byte[] compressed = FL2.Compress(origin, 10);
            byte[] decompressed = FL2.Decompress(compressed);
            Assert.IsTrue(origin.SequenceEqual(decompressed), "The byte arrays are not equal.");
        }

        [TestMethod]
        public void SimpleMT()
        {
            byte[] src = new byte[4096 * 1024];
            new Random().NextBytes(src);

            byte[] compressed = FL2.CompressMT(src, 10, 0);
            byte[] decompressed = FL2.DecompressMT(compressed, 0);
            Assert.IsTrue(src.SequenceEqual(decompressed), "The byte arrays are not equal.");
        }

        [TestMethod]
        public void FindDecompressedSize()
        {
            byte[] data = File.ReadAllBytes("Resources/dummy.fl2");
            nuint code = FL2.FindDecompressedSize(data);
            Assert.AreEqual(581048, (long)code);
            code = FL2.FindDecompressedSize("Resources/dummy.fl2");
            Assert.AreEqual(581048, (long)code);
        }

        [TestMethod]
        public void GetPresetParams()
        {
            CompressionParameters parameters;
            for (int level = 0; level <= 10; level++)
            {
                parameters = FL2.GetPresetLevelParameters(level, 0);
            }

            var exception = Assert.ThrowsExactly<FL2Exception>(() =>
            {
                parameters = FL2.GetPresetLevelParameters(-1, 0);
            });
            Assert.AreEqual(FL2ErrorCode.ParameterOutOfBound, exception.ErrorCode);
            exception = Assert.ThrowsExactly<FL2Exception>(() =>
            {
                parameters = FL2.GetPresetLevelParameters(11, 0);
            });
            Assert.AreEqual(FL2ErrorCode.ParameterOutOfBound, exception.ErrorCode);
            Console.WriteLine($"Max Level{FL2.CompressionLevelMax}");
            Console.WriteLine($"Max High Level{FL2.HighCompressionLevelMax}");
        }
    }
}