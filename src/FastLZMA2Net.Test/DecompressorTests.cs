using FastLZMA2Net;

namespace Test
{
    [TestClass]
    public class DecompressorTests
    {
        private static byte[] _originData = null!;
        private static byte[] _compressedData = null!;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            _originData = File.ReadAllBytes("Resources/dummy.raw");
            _compressedData = FL2.Compress(_originData, 1);
        }

        #region Constructor

        [TestMethod]
        public void WhenCreateDefaultDecompressorThenSucceeds()
        {
            using Decompressor decompressor = new Decompressor();
            Assert.IsNotNull(decompressor);
        }

        [TestMethod]
        public void WhenCreateSTDecompressorThenSucceeds()
        {
            using Decompressor decompressor = new Decompressor(nbThreads: 1);
            Assert.IsNotNull(decompressor);
        }

        [TestMethod]
        public void WhenCreateMTDecompressorThenThreadCountIsPositive()
        {
            using Decompressor decompressor = new Decompressor(nbThreads: 0);
            Assert.IsTrue(decompressor.ThreadCount > 0);
        }

        #endregion

        #region Decompress byte[]

        [TestMethod]
        public void WhenDecompressByteArrayThenReturnsOriginalData()
        {
            using Decompressor decompressor = new Decompressor();
            byte[] decompressed = decompressor.Decompress(_compressedData);
            CollectionAssert.AreEqual(_originData, decompressed);
        }

        [TestMethod]
        public void WhenDecompressMTByteArrayThenReturnsOriginalData()
        {
            using Decompressor decompressor = new Decompressor(nbThreads: 0);
            byte[] decompressed = decompressor.Decompress(_compressedData);
            CollectionAssert.AreEqual(_originData, decompressed);
        }

        [TestMethod]
        public void WhenDecompressNullByteArrayThenThrowsArgumentNullException()
        {
            using Decompressor decompressor = new Decompressor();
            Assert.ThrowsExactly<ArgumentNullException>(() => decompressor.Decompress((byte[])null!));
        }

        [TestMethod]
        public void WhenDecompressCorruptDataThenThrowsFL2Exception()
        {
            using Decompressor decompressor = new Decompressor();
            byte[] corrupt = new byte[64];
            new Random(99).NextBytes(corrupt);
            Assert.ThrowsExactly<FL2Exception>(() => decompressor.Decompress(corrupt));
        }

        #endregion

        #region Decompress Span

        [TestMethod]
        public void WhenDecompressSpanThenReturnsOriginalData()
        {
            using Decompressor decompressor = new Decompressor();
            byte[] decompressed = decompressor.Decompress(_compressedData.AsSpan());
            CollectionAssert.AreEqual(_originData, decompressed);
        }

        #endregion

        #region DecompressAsync

        [TestMethod]
        public async Task WhenDecompressAsyncByteArrayThenReturnsOriginalData()
        {
            using Decompressor decompressor = new Decompressor();
            byte[] decompressed = await decompressor.DecompressAsync(_compressedData);
            CollectionAssert.AreEqual(_originData, decompressed);
        }

        [TestMethod]
        public async Task WhenDecompressAsyncMemoryThenReturnsOriginalData()
        {
            using Decompressor decompressor = new Decompressor();
            byte[] decompressed = await decompressor.DecompressAsync(_compressedData.AsMemory());
            CollectionAssert.AreEqual(_originData, decompressed);
        }

        [TestMethod]
        public async Task WhenDecompressAsyncWithCancelledTokenThenThrowsOperationCanceledException()
        {
            using Decompressor decompressor = new Decompressor();
            using CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsExactlyAsync<TaskCanceledException>(
                () => decompressor.DecompressAsync(_compressedData, cts.Token));
        }

        #endregion

        #region Init

        [TestMethod]
        public void WhenInitWithDictSizePropertyThenSucceeds()
        {
            byte[] src = new byte[64 * 1024];
            new Random(42).NextBytes(src);

            using Compressor compressor = new Compressor();
            compressor.Compress(src);
            byte prop = compressor.DictSizeProperty;

            using Decompressor decompressor = new Decompressor();
            decompressor.Init(prop);
        }

        #endregion

        #region Cross-method Roundtrip

        [TestMethod]
        public void WhenCompressorProducesDataThenDecompressorRecoversIt()
        {
            using Compressor compressor = new Compressor(nbThreads: 0);
            using Decompressor decompressor = new Decompressor(nbThreads: 0);

            byte[] src = new byte[128 * 1024];
            new Random(42).NextBytes(src);

            byte[] compressed = compressor.Compress(src, 5);
            byte[] decompressed = decompressor.Decompress(compressed);

            CollectionAssert.AreEqual(src, decompressed);
        }

        [TestMethod]
        public void WhenStaticCompressProducesDataThenDecompressorRecoversIt()
        {
            using Decompressor decompressor = new Decompressor();

            byte[] src = new byte[128 * 1024];
            new Random(42).NextBytes(src);

            byte[] compressed = FL2.Compress(src, 5);
            byte[] decompressed = decompressor.Decompress(compressed);

            CollectionAssert.AreEqual(src, decompressed);
        }

        #endregion

        #region Dispose Guards

        [TestMethod]
        public void WhenDecompressAfterDisposeThenThrowsObjectDisposedException()
        {
            Decompressor decompressor = new Decompressor();
            decompressor.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(() => decompressor.Decompress(new byte[64]));
        }

        [TestMethod]
        public void WhenAccessThreadCountAfterDisposeThenThrowsObjectDisposedException()
        {
            Decompressor decompressor = new Decompressor();
            decompressor.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(() => _ = decompressor.ThreadCount);
        }

        [TestMethod]
        public void WhenInitAfterDisposeThenThrowsObjectDisposedException()
        {
            Decompressor decompressor = new Decompressor();
            decompressor.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(() => decompressor.Init(0));
        }

        [TestMethod]
        public void WhenDoubleDisposeThenDoesNotThrow()
        {
            Decompressor decompressor = new Decompressor();
            decompressor.Dispose();
            decompressor.Dispose();
        }

        #endregion
    }
}
