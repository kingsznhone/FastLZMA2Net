using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            for (int i = 0; i < 5; i++)
            {
                byte[] src = new byte[4096 * 1024];
                new Random().NextBytes(src);

                byte[] compressed = compressorMT.Compress(src, 10);
                
                byte prop = compressorMT.DictSizeProperty;
                byte[] decompressed = decompressorMT.Decompress(compressed);
                Assert.IsTrue(src.SequenceEqual(decompressed), "The byte arrays are not equal.");
            }
        }


        [TestMethod]
        public void CompressorST()
        {
            Compressor compressorST = new Compressor();

            for (int i = 0; i < 5; i++)
            {
                byte[] src = new byte[4096 * 1024];
                new Random().NextBytes(src);

                byte[] compressed = compressorST.Compress(src, 10);
                byte[] decompressed = FL2.Decompress(compressed);
                Assert.IsTrue(src.SequenceEqual(decompressed), "The byte arrays are not equal.");
            }
        }

        [TestMethod]
        public void CompressorMT()
        {
            Compressor compressorMT = new Compressor(0);
            Debug.WriteLine($"Thread Count: {compressorMT.ThreadCount}");
            Debug.WriteLine($"Dict Size Property: {compressorMT.DictSizeProperty}");
            for (int i = 0; i < 5; i++)
            {
                byte[] src = new byte[4096 * 1024];
                new Random().NextBytes(src);

                byte[] compressed = compressorMT.Compress(src, 10);
                byte[] decompressed = FL2.Decompress(compressed);

                Assert.IsTrue(src.SequenceEqual(decompressed), "The byte arrays are not equal.");
            }
        }
        [TestMethod]
        public void SetDictSize()
        {
            Compressor compressorST = new Compressor();
            compressorST.DictionarySize = FL2.DictSizeMin;
            compressorST.DictionarySize = FL2.DictSizeMax;
            var exception = Assert.ThrowsException<FL2Exception>(() =>
            {
                compressorST.DictionarySize = FL2.DictSizeMin >> 1 ;
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
            for (int i = 0; i < 5; i++)
            {
                byte[] src = new byte[4096 * 1024];
                new Random().NextBytes(src);

                byte[] compressed = FL2.Compress(src, 10);
                byte[] decompressed = decompressorST.Decompress(compressed); ;
                Assert.IsTrue(src.SequenceEqual(decompressed), "The byte arrays are not equal.");
            }
        }

        [TestMethod]
        public void DecompressorMT()
        {
            Decompressor decompressorMT = new Decompressor(0);
            for (int i = 0; i < 5; i++)
            {
                byte[] src = new byte[4096 * 1024];
                new Random().NextBytes(src);

                byte[] compressed = FL2.Compress(src, 10);
                byte[] decompressed = decompressorMT.Decompress(compressed); ;
                Assert.IsTrue(src.SequenceEqual(decompressed), "The byte arrays are not equal.");
            }
        }
    }
}
