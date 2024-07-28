using System.Diagnostics;
using FastLZMA2Net;

namespace Test
{
    [TestClass]
    public class ContextTest
    {
        [TestMethod]
        public void Union()
        {
            Compressor compressorMT = new Compressor();
            Decompressor decompressorMT = new Decompressor(0);
            byte[] src = new byte[4096 * 1024];
            new Random().NextBytes(src);

            byte[] compressed = compressorMT.Compress(src);

            byte prop = compressorMT.DictSizeProperty;
            byte[] decompressed = decompressorMT.Decompress(compressed);
            Assert.IsTrue(src.SequenceEqual(decompressed), "The byte arrays are not equal.");
        }

        [TestMethod]
        public void CompressorST()
        {
            Compressor compressorST = new Compressor();
            byte[] src = new byte[4096 * 1024];
            new Random().NextBytes(src);

            byte[] compressed = compressorST.Compress(src);
            byte[] decompressed = FL2.Decompress(compressed);
            Assert.IsTrue(src.SequenceEqual(decompressed), "The byte arrays are not equal.");
        }

        [TestMethod]
        public void CompressorMT()
        {
            Compressor compressorMT = new Compressor(0);
            Debug.WriteLine($"Thread Count: {compressorMT.ThreadCount}");
            Debug.WriteLine($"Dict Size Property: {compressorMT.DictSizeProperty}");

            byte[] src = new byte[4096 * 1024];
            new Random().NextBytes(src);

            byte[] compressed = compressorMT.Compress(src, 10);
            byte[] decompressed = FL2.Decompress(compressed);

            Assert.IsTrue(src.SequenceEqual(decompressed), "The byte arrays are not equal.");
        }

        [TestMethod]
        public void DirectCompress()
        {
            Compressor compressor = new Compressor();
            nuint length = compressor.Compress("Resources/dummy.raw", "Resources/dummy.test");
            byte[] compressed = File.ReadAllBytes("Resources/dummy.test");
            byte[] recovered = FL2.Decompress(compressed);
            byte[] origin = File.ReadAllBytes("Resources/dummy.raw");
            File.Delete("Resources/dummy.test");
            Assert.IsTrue(recovered.SequenceEqual(origin));
        }

        [TestMethod]
        public void SetDictSize()
        {
            Compressor compressorST = new Compressor();
            compressorST.DictionarySize = FL2.DictSizeMin;
            compressorST.DictionarySize = FL2.DictSizeMax;
            var exception = Assert.ThrowsException<FL2Exception>(() =>
            {
                compressorST.DictionarySize = FL2.DictSizeMin >> 1;
            });
            Assert.AreEqual(exception.ErrorCode, FL2ErrorCode.parameter_outOfBound);
            exception = Assert.ThrowsException<FL2Exception>(() =>
            {
                compressorST.DictionarySize = FL2.DictSizeMax << 1;
            });
            Assert.AreEqual(exception.ErrorCode, FL2ErrorCode.parameter_outOfBound);
        }

        [TestMethod]
        public void SetLevel()
        {
            Compressor compressorST = new Compressor();
            for (int i = 1; i <= 10; i++)
            {
                compressorST.CompressLevel = i;
            }

            var exception = Assert.ThrowsException<FL2Exception>(() =>
            {
                compressorST.CompressLevel = -1;
            });
            Assert.AreEqual(exception.ErrorCode, FL2ErrorCode.parameter_outOfBound);
            exception = Assert.ThrowsException<FL2Exception>(() =>
            {
                compressorST.CompressLevel = 0;
            });
            Assert.AreEqual(exception.ErrorCode, FL2ErrorCode.parameter_outOfBound);
            exception = Assert.ThrowsException<FL2Exception>(() =>
            {
                compressorST.CompressLevel = 11;
            });
            Assert.AreEqual(exception.ErrorCode, FL2ErrorCode.parameter_outOfBound);
        }

        [TestMethod]
        public void DecompressorST()
        {
            Decompressor decompressorST = new Decompressor();
            byte[] src = new byte[4096 * 1024];
            new Random().NextBytes(src);

            byte[] compressed = FL2.Compress(src, 10);
            byte[] decompressed = decompressorST.Decompress(compressed); ;
            Assert.IsTrue(src.SequenceEqual(decompressed), "The byte arrays are not equal.");
        }

        [TestMethod]
        public void DecompressorMT()
        {
            Decompressor decompressorMT = new Decompressor(0);

            byte[] src = new byte[4096 * 1024];
            new Random().NextBytes(src);

            byte[] compressed = FL2.Compress(src, 10);
            byte[] decompressed = decompressorMT.Decompress(compressed); ;
            Assert.IsTrue(src.SequenceEqual(decompressed), "The byte arrays are not equal.");
        }
    }
}