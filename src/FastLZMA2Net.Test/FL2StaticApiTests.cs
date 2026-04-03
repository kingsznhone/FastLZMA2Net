using FastLZMA2Net;

namespace Test
{
    [TestClass]
    public class FL2StaticApiTests
    {
        private static byte[] _originData = null!;
        private static byte[] _compressedData = null!;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            _originData = File.ReadAllBytes("Resources/dummy.raw");
            _compressedData = FL2.Compress(_originData, 1);
        }

        #region Constants and Properties

        [TestMethod]
        public void WhenAccessVersionThenReturnsValidVersion()
        {
            Assert.IsNotNull(FL2.Version);
            Assert.IsTrue(FL2.Version.Major >= 0);
        }

        [TestMethod]
        public void WhenAccessVersionNumberThenReturnsPositiveValue()
        {
            Assert.IsTrue(FL2.VersionNumber > 0);
        }

        [TestMethod]
        public void WhenAccessVersionStringThenReturnsNonEmptyString()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(FL2.VersionString));
        }

        [TestMethod]
        public void WhenAccessDictSizeMinThenReturnsOneMB()
        {
            Assert.AreEqual(1 << 20, FL2.DictSizeMin);
        }

        [TestMethod]
        public void WhenAccessDictSizeMaxThenReturnsPlatformSpecificValue()
        {
            int expected = nint.Size == 4 ? 1 << 27 : 1 << 30;
            Assert.AreEqual(expected, FL2.DictSizeMax);
        }

        [TestMethod]
        public void WhenAccessCompressionLevelMaxThenReturnsPositiveValue()
        {
            Assert.IsTrue(FL2.CompressionLevelMax > 0);
        }

        [TestMethod]
        public void WhenAccessHighCompressionLevelMaxThenReturnsPositiveValue()
        {
            Assert.IsTrue(FL2.HighCompressionLevelMax > 0);
        }

        [TestMethod]
        public void WhenAccessConstantRangesThenMinIsLessThanOrEqualToMax()
        {
            Assert.IsTrue(FL2.BlockOverlapMin <= FL2.BlockOverlapMax);
            Assert.IsTrue(FL2.ResetIntervalMin <= FL2.ResetIntervalMax);
            Assert.IsTrue(FL2.BufferResizeMin <= FL2.BufferResizeMax);
            Assert.IsTrue(FL2.ChainLogMin <= FL2.ChainLogMax);
            Assert.IsTrue(FL2.HybridCyclesMin <= FL2.HybridCyclesMax);
            Assert.IsTrue(FL2.SearchDepthMin <= FL2.SearchDepthMax);
            Assert.IsTrue(FL2.FastLengthMin <= FL2.FastLengthMax);
            Assert.IsTrue(FL2.LCMin <= FL2.LCMax);
            Assert.IsTrue(FL2.LPMin <= FL2.LPMax);
            Assert.IsTrue(FL2.PBMin <= FL2.PBMax);
        }

        #endregion

        #region GetPresetLevelParameters

        [TestMethod]
        [DataRow(1)]
        [DataRow(5)]
        [DataRow(10)]
        public void WhenGetPresetLevelParametersThenReturnsValidParameters(int level)
        {
            CompressionParameters parameters = FL2.GetPresetLevelParameters(level, 0);
            Assert.IsTrue(parameters.DictionarySize > 0);
        }

        [TestMethod]
        public void WhenGetPresetLevelParametersWithNegativeLevelThenThrowsFL2Exception()
        {
            var ex = Assert.ThrowsExactly<FL2Exception>(() => FL2.GetPresetLevelParameters(-1, 0));
            Assert.AreEqual(FL2ErrorCode.ParameterOutOfBound, ex.ErrorCode);
        }

        [TestMethod]
        public void WhenGetPresetLevelParametersWithExcessiveLevelThenThrowsFL2Exception()
        {
            var ex = Assert.ThrowsExactly<FL2Exception>(() => FL2.GetPresetLevelParameters(11, 0));
            Assert.AreEqual(FL2ErrorCode.ParameterOutOfBound, ex.ErrorCode);
        }

        #endregion

        #region FindCompressBound

        [TestMethod]
        public void WhenFindCompressBoundByNuintThenReturnsBoundLargerThanInput()
        {
            nuint bound = FL2.FindCompressBound(1024);
            Assert.IsTrue(bound >= 1024);
        }

        [TestMethod]
        public void WhenFindCompressBoundByByteArrayThenReturnsBoundLargerThanInput()
        {
            byte[] src = new byte[1024];
            nuint bound = FL2.FindCompressBound(src);
            Assert.IsTrue(bound >= (nuint)src.Length);
        }

        [TestMethod]
        public void WhenFindCompressBoundBySpanThenReturnsBoundLargerThanInput()
        {
            ReadOnlySpan<byte> src = new byte[1024];
            nuint bound = FL2.FindCompressBound(src);
            Assert.IsTrue(bound >= 1024);
        }

        [TestMethod]
        public void WhenFindCompressBoundWithNullArrayThenThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => FL2.FindCompressBound((byte[])null!));
        }

        #endregion

        #region FindDecompressedSize

        [TestMethod]
        public void WhenFindDecompressedSizeByByteArrayThenReturnsExpectedSize()
        {
            nuint size = FL2.FindDecompressedSize(_compressedData);
            Assert.AreEqual((nuint)_originData.Length, size);
        }

        [TestMethod]
        public void WhenFindDecompressedSizeByFilePathThenReturnsExpectedSize()
        {
            string path = "Resources/dummy.fl2";
            File.WriteAllBytes(path, _compressedData);
            nuint size = FL2.FindDecompressedSize(path);
            Assert.AreEqual((nuint)_originData.Length, size);
        }

        [TestMethod]
        public void WhenFindDecompressedSizeWithNullArrayThenThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => FL2.FindDecompressedSize((byte[])null!));
        }

        [TestMethod]
        public void WhenFindDecompressedSizeWithNullPathThenThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => FL2.FindDecompressedSize((string)null!));
        }

        [TestMethod]
        public void WhenFindDecompressedSizeWithEmptyPathThenThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(() => FL2.FindDecompressedSize(""));
        }

        [TestMethod]
        public void WhenFindDecompressedSizeWithMissingFileThenThrowsFileNotFoundException()
        {
            Assert.ThrowsExactly<FileNotFoundException>(() => FL2.FindDecompressedSize("nonexistent.fl2"));
        }

        #endregion

        #region Compress / Decompress byte[]

        [TestMethod]
        [DataRow(1)]
        [DataRow(6)]
        [DataRow(10)]
        public void WhenCompressAndDecompressByteArrayThenRoundTripsCorrectly(int level)
        {
            byte[] src = new byte[64 * 1024];
            new Random(42).NextBytes(src);

            byte[] compressed = FL2.Compress(src, level);
            byte[] decompressed = FL2.Decompress(compressed);

            CollectionAssert.AreEqual(src, decompressed);
        }

        [TestMethod]
        public void WhenCompressWithNullArrayThenThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => FL2.Compress(null!, 1));
        }

        [TestMethod]
        public void WhenDecompressWithNullArrayThenThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => FL2.Decompress((byte[])null!));
        }

        #endregion

        #region Compress / Decompress Span

        [TestMethod]
        public void WhenCompressAndDecompressSpanThenRoundTripsCorrectly()
        {
            byte[] src = new byte[64 * 1024];
            new Random(42).NextBytes(src);

            byte[] compressed = FL2.Compress(src.AsSpan(), 6);
            byte[] decompressed = FL2.Decompress(compressed.AsSpan());

            CollectionAssert.AreEqual(src, decompressed);
        }

        #endregion

        #region CompressMT / DecompressMT byte[]

        [TestMethod]
        public void WhenCompressMTAndDecompressMTByteArrayThenRoundTripsCorrectly()
        {
            byte[] src = new byte[256 * 1024];
            new Random(42).NextBytes(src);

            byte[] compressed = FL2.CompressMT(src, 6, 0);
            byte[] decompressed = FL2.DecompressMT(compressed, 0);

            CollectionAssert.AreEqual(src, decompressed);
        }

        [TestMethod]
        public void WhenCompressMTWithNullArrayThenThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => FL2.CompressMT(null!, 1, 0));
        }

        [TestMethod]
        public void WhenDecompressMTWithNullArrayThenThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => FL2.DecompressMT((byte[])null!, 0));
        }

        #endregion

        #region CompressMT / DecompressMT Span

        [TestMethod]
        public void WhenCompressMTAndDecompressMTSpanThenRoundTripsCorrectly()
        {
            byte[] src = new byte[256 * 1024];
            new Random(42).NextBytes(src);

            byte[] compressed = FL2.CompressMT(src.AsSpan(), 6, 0);
            byte[] decompressed = FL2.DecompressMT(compressed.AsSpan(), 0);

            CollectionAssert.AreEqual(src, decompressed);
        }

        #endregion

        #region Cross-method Compatibility

        [TestMethod]
        public void WhenCompressedWithSTThenDecompressableWithMT()
        {
            byte[] src = new byte[64 * 1024];
            new Random(42).NextBytes(src);

            byte[] compressed = FL2.Compress(src, 6);
            byte[] decompressed = FL2.DecompressMT(compressed, 0);

            CollectionAssert.AreEqual(src, decompressed);
        }

        [TestMethod]
        public void WhenCompressedWithMTThenDecompressableWithST()
        {
            byte[] src = new byte[64 * 1024];
            new Random(42).NextBytes(src);

            byte[] compressed = FL2.CompressMT(src, 6, 0);
            byte[] decompressed = FL2.Decompress(compressed);

            CollectionAssert.AreEqual(src, decompressed);
        }

        [TestMethod]
        public void WhenCompressedWithByteArrayThenDecompressableWithSpan()
        {
            byte[] src = new byte[64 * 1024];
            new Random(42).NextBytes(src);

            byte[] compressed = FL2.Compress(src, 6);
            byte[] decompressed = FL2.Decompress(compressed.AsSpan());

            CollectionAssert.AreEqual(src, decompressed);
        }

        #endregion

        #region EstimateMemoryUsage

        [TestMethod]
        public void WhenEstimateCompressMemoryUsageThenReturnsPositiveValue()
        {
            nuint usage = FL2.EstimateCompressMemoryUsage(6, 0);
            Assert.IsTrue(usage > 0);
        }

        [TestMethod]
        public void WhenEstimateDecompressMemoryUsageThenReturnsPositiveValue()
        {
            nuint usage = FL2.EstimateDecompressMemoryUsage(0);
            Assert.IsTrue(usage > 0);
        }

        [TestMethod]
        public void WhenEstimateCompressMemoryUsageByParamsThenReturnsPositiveValue()
        {
            CompressionParameters parameters = FL2.GetPresetLevelParameters(6, 0);
            nuint usage = FL2.EstimateCompressMemoryUsage(parameters, 0);
            Assert.IsTrue(usage > 0);
        }

        #endregion

        #region Empty Data

        [TestMethod]
        public void WhenCompressEmptyArrayThenProducesValidOutput()
        {
            byte[] src = [];
            byte[] compressed = FL2.Compress(src, 1);
            Assert.IsTrue(compressed.Length > 0);
        }

        #endregion
    }
}
