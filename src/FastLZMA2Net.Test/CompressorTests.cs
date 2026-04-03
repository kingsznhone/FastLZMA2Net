using FastLZMA2Net;

namespace Test
{
    [TestClass]
    public class CompressorTests
    {
        private static byte[] _originData = null!;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            _originData = File.ReadAllBytes("Resources/dummy.raw");
        }

        #region Constructor

        [TestMethod]
        public void WhenCreateDefaultCompressorThenSucceeds()
        {
            using Compressor compressor = new Compressor();
            Assert.IsTrue(compressor.CompressLevel > 0);
        }

        [TestMethod]
        public void WhenCreateCompressorWithCustomLevelThenLevelIsSet()
        {
            using Compressor compressor = new Compressor(compressLevel: 3);
            Assert.AreEqual(3, compressor.CompressLevel);
        }

        [TestMethod]
        public void WhenCreateMTCompressorThenThreadCountIsPositive()
        {
            using Compressor compressor = new Compressor(nbThreads: 0);
            Assert.IsTrue(compressor.ThreadCount > 0);
        }

        [TestMethod]
        public void WhenCreateSTCompressorThenThreadCountIsOne()
        {
            using Compressor compressor = new Compressor(nbThreads: 1);
            Assert.AreEqual(1u, compressor.ThreadCount);
        }

        #endregion Constructor

        #region Compress byte[]

        [TestMethod]
        public void WhenCompressByteArrayThenRoundTripsCorrectly()
        {
            using Compressor compressor = new Compressor();
            byte[] compressed = compressor.Compress(_originData);
            byte[] decompressed = FL2.Decompress(compressed);
            CollectionAssert.AreEqual(_originData, decompressed);
        }

        [TestMethod]
        public void WhenCompressByteArrayWithLevelThenRoundTripsCorrectly()
        {
            using Compressor compressor = new Compressor();
            byte[] compressed = compressor.Compress(_originData, 3);
            byte[] decompressed = FL2.Decompress(compressed);
            CollectionAssert.AreEqual(_originData, decompressed);
        }

        [TestMethod]
        public void WhenCompressNullByteArrayThenThrowsArgumentNullException()
        {
            using Compressor compressor = new Compressor();
            Assert.ThrowsExactly<ArgumentNullException>(() => compressor.Compress((byte[])null!));
        }

        #endregion Compress byte[]

        #region Compress Span

        [TestMethod]
        public void WhenCompressSpanThenRoundTripsCorrectly()
        {
            using Compressor compressor = new Compressor();
            byte[] compressed = compressor.Compress(_originData.AsSpan());
            byte[] decompressed = FL2.Decompress(compressed);
            CollectionAssert.AreEqual(_originData, decompressed);
        }

        [TestMethod]
        public void WhenCompressSpanWithLevelThenRoundTripsCorrectly()
        {
            using Compressor compressor = new Compressor();
            byte[] compressed = compressor.Compress(_originData.AsSpan(), 3);
            byte[] decompressed = FL2.Decompress(compressed);
            CollectionAssert.AreEqual(_originData, decompressed);
        }

        #endregion Compress Span

        #region CompressAsync

        [TestMethod]
        public async Task WhenCompressAsyncByteArrayThenRoundTripsCorrectly()
        {
            using Compressor compressor = new Compressor();
            byte[] compressed = await compressor.CompressAsync(_originData);
            byte[] decompressed = FL2.Decompress(compressed);
            CollectionAssert.AreEqual(_originData, decompressed);
        }

        [TestMethod]
        public async Task WhenCompressAsyncWithLevelThenRoundTripsCorrectly()
        {
            using Compressor compressor = new Compressor();
            byte[] compressed = await compressor.CompressAsync(_originData, 3);
            byte[] decompressed = FL2.Decompress(compressed);
            CollectionAssert.AreEqual(_originData, decompressed);
        }

        [TestMethod]
        public async Task WhenCompressAsyncMemoryThenRoundTripsCorrectly()
        {
            using Compressor compressor = new Compressor();
            byte[] compressed = await compressor.CompressAsync(_originData.AsMemory());
            byte[] decompressed = FL2.Decompress(compressed);
            CollectionAssert.AreEqual(_originData, decompressed);
        }

        [TestMethod]
        public async Task WhenCompressAsyncWithCancelledTokenThenThrowsOperationCanceledException()
        {
            using Compressor compressor = new Compressor();
            using CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsExactlyAsync<TaskCanceledException>(
                () => compressor.CompressAsync(_originData, cts.Token));
        }

        #endregion CompressAsync

        #region File Compress

        [TestMethod]
        public void WhenCompressFileThenOutputMatchesDecompressed()
        {
            string srcPath = "Resources/dummy.raw";
            string dstPath = "Resources/compressor_test_output.fl2";
            using Compressor compressor = new Compressor();
            nuint length = compressor.Compress(srcPath, dstPath);
            Assert.IsTrue(length > 0);

            byte[] compressed = File.ReadAllBytes(dstPath);
            byte[] recovered = FL2.Decompress(compressed);
            CollectionAssert.AreEqual(_originData, recovered);
            File.Delete(dstPath);
        }

        [TestMethod]
        public void WhenCompressFileWithNullSrcPathThenThrowsArgumentException()
        {
            using Compressor compressor = new Compressor();
            Assert.ThrowsExactly<ArgumentNullException>(() => compressor.Compress((string)null!, "dst.fl2"));
        }

        [TestMethod]
        public void WhenCompressFileWithNullDstPathThenThrowsArgumentException()
        {
            using Compressor compressor = new Compressor();
            Assert.ThrowsExactly<ArgumentNullException>(() => compressor.Compress("Resources/dummy.raw", (string)null!));
        }

        #endregion File Compress

        #region Parameters

        [TestMethod]
        [DataRow(1)]
        [DataRow(5)]
        [DataRow(10)]
        public void WhenSetCompressLevelThenGetReturnsNewValue(int level)
        {
            using Compressor compressor = new Compressor();
            compressor.CompressLevel = level;
            Assert.AreEqual(level, compressor.CompressLevel);
        }

        [TestMethod]
        public void WhenSetCompressLevelOutOfRangeThenThrowsFL2Exception()
        {
            using Compressor compressor = new Compressor();
            var ex = Assert.ThrowsExactly<FL2Exception>(() => compressor.CompressLevel = 0);
            Assert.AreEqual(FL2ErrorCode.ParameterOutOfBound, ex.ErrorCode);
        }

        [TestMethod]
        public void WhenSetDictionarySizeMinThenSucceeds()
        {
            using Compressor compressor = new Compressor();
            compressor.DictionarySize = FL2.DictSizeMin;
            Assert.AreEqual(FL2.DictSizeMin, compressor.DictionarySize);
        }

        [TestMethod]
        public void WhenSetDictionarySizeMaxThenSucceeds()
        {
            using Compressor compressor = new Compressor();
            compressor.DictionarySize = FL2.DictSizeMax;
            Assert.AreEqual(FL2.DictSizeMax, compressor.DictionarySize);
        }

        [TestMethod]
        public void WhenSetDictionarySizeBelowMinThenThrowsFL2Exception()
        {
            using Compressor compressor = new Compressor();
            var ex = Assert.ThrowsExactly<FL2Exception>(() => compressor.DictionarySize = FL2.DictSizeMin >> 1);
            Assert.AreEqual(FL2ErrorCode.ParameterOutOfBound, ex.ErrorCode);
        }

        [TestMethod]
        public void WhenGetDictSizePropertyThenReturnsNonZero()
        {
            using Compressor compressor = new Compressor();
            Assert.IsTrue(compressor.DictSizeProperty > 0);
        }

        [TestMethod]
        public void WhenSetSearchDepthThenGetReturnsNewValue()
        {
            using Compressor compressor = new Compressor();
            compressor.SearchDepth = FL2.SearchDepthMin;
            Assert.AreEqual(FL2.SearchDepthMin, compressor.SearchDepth);
        }

        [TestMethod]
        public void WhenSetFastLengthThenGetReturnsNewValue()
        {
            using Compressor compressor = new Compressor();
            compressor.FastLength = FL2.FastLengthMin;
            Assert.AreEqual(FL2.FastLengthMin, compressor.FastLength);
        }

        #endregion Parameters

        #region Dispose Guards

        [TestMethod]
        public void WhenCompressAfterDisposeThenThrowsObjectDisposedException()
        {
            Compressor compressor = new Compressor();
            compressor.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(() => compressor.Compress(new byte[1024]));
        }

        [TestMethod]
        public void WhenAccessThreadCountAfterDisposeThenThrowsObjectDisposedException()
        {
            Compressor compressor = new Compressor();
            compressor.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(() => _ = compressor.ThreadCount);
        }

        [TestMethod]
        public void WhenAccessDictSizePropertyAfterDisposeThenThrowsObjectDisposedException()
        {
            Compressor compressor = new Compressor();
            compressor.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(() => _ = compressor.DictSizeProperty);
        }

        [TestMethod]
        public void WhenSetParameterAfterDisposeThenThrowsObjectDisposedException()
        {
            Compressor compressor = new Compressor();
            compressor.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(
                () => compressor.SetParameter(FL2Parameter.CompressionLevel, 5));
        }

        [TestMethod]
        public void WhenGetParameterAfterDisposeThenThrowsObjectDisposedException()
        {
            Compressor compressor = new Compressor();
            compressor.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(
                () => compressor.GetParameter(FL2Parameter.CompressionLevel));
        }

        [TestMethod]
        public void WhenDoubleDisposeThenDoesNotThrow()
        {
            Compressor compressor = new Compressor();
            compressor.Dispose();
            compressor.Dispose();
        }

        #endregion Dispose Guards

        #region MT Roundtrip

        [TestMethod]
        public void WhenCompressWithMTCompressorThenSTDecompressSucceeds()
        {
            using Compressor compressor = new Compressor(nbThreads: 0);
            byte[] compressed = compressor.Compress(_originData, 5);
            byte[] decompressed = FL2.Decompress(compressed);
            CollectionAssert.AreEqual(_originData, decompressed);
        }

        #endregion MT Roundtrip
    }
}