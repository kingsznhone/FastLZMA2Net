using FastLZMA2Net;

namespace Test
{
    [TestClass]
    public class DecompressStreamTests
    {
        private static byte[] _originData = null!;
        private static byte[] _compressedData = null!;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            _originData = File.ReadAllBytes("Resources/dummy.raw");
            _compressedData = FL2.Compress(_originData, 1);
        }

        private static MemoryStream MakeCompressedStream() => new MemoryStream(_compressedData);

        #region Read One-shot

        [TestMethod]
        public void WhenReadOneshotByteArrayThenReturnsOriginalData()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms);
            byte[] buffer = new byte[_originData.Length];
            int bytesRead = ds.Read(buffer, 0, buffer.Length);
            CollectionAssert.AreEqual(_originData, buffer[0..bytesRead]);
        }

        [TestMethod]
        public void WhenReadOneshotSpanThenReturnsOriginalData()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms);
            byte[] buffer = new byte[_originData.Length];
            int bytesRead = ds.Read(buffer.AsSpan());
            CollectionAssert.AreEqual(_originData, buffer[0..bytesRead]);
        }

        #endregion Read One-shot

        #region Read Blocked

        [TestMethod]
        public void WhenReadBlockedByteArrayThenReturnsOriginalData()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms);
            using MemoryStream recoveryStream = new MemoryStream();
            byte[] buffer = new byte[1024];
            int reads;
            while ((reads = ds.Read(buffer, 0, buffer.Length)) != 0)
            {
                recoveryStream.Write(buffer, 0, reads);
            }
            CollectionAssert.AreEqual(_originData, recoveryStream.ToArray());
        }

        [TestMethod]
        public void WhenReadBlockedSpanThenReturnsOriginalData()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms);
            using MemoryStream recoveryStream = new MemoryStream();
            byte[] buffer = new byte[4096];
            int reads;
            while ((reads = ds.Read(buffer.AsSpan())) != 0)
            {
                recoveryStream.Write(buffer, 0, reads);
            }
            CollectionAssert.AreEqual(_originData, recoveryStream.ToArray());
        }

        #endregion Read Blocked

        #region ReadAsync

        [TestMethod]
        public async Task WhenReadAsyncByteArrayThenReturnsOriginalData()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms);
            byte[] buffer = new byte[_originData.Length];
            int bytesRead = await ds.ReadAsync(buffer, 0, buffer.Length);
            CollectionAssert.AreEqual(_originData, buffer[0..bytesRead]);
        }

        [TestMethod]
        public async Task WhenReadAsyncMemoryThenReturnsOriginalData()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms);
            byte[] buffer = new byte[_originData.Length];
            int bytesRead = await ds.ReadAsync(buffer.AsMemory());
            CollectionAssert.AreEqual(_originData, buffer[0..bytesRead]);
        }

        [TestMethod]
        public async Task WhenReadAsyncBlockedByteArrayThenReturnsOriginalData()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms);
            using MemoryStream recoveryStream = new MemoryStream();
            byte[] buffer = new byte[1024];
            int reads;
            while ((reads = await ds.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                recoveryStream.Write(buffer, 0, reads);
            }
            CollectionAssert.AreEqual(_originData, recoveryStream.ToArray());
        }

        #endregion ReadAsync

        #region CopyTo

        [TestMethod]
        public void WhenCopyToStreamThenReturnsOriginalData()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms);
            using MemoryStream recoveryStream = new MemoryStream();
            ds.CopyTo(recoveryStream);
            CollectionAssert.AreEqual(_originData, recoveryStream.ToArray());
        }

        [TestMethod]
        public void WhenCopyToStreamWithCustomBufferSizeThenReturnsOriginalData()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms);
            using MemoryStream recoveryStream = new MemoryStream();
            ds.CopyTo(recoveryStream, 1024 * 1024);
            CollectionAssert.AreEqual(_originData, recoveryStream.ToArray());
        }

        [TestMethod]
        public void WhenCopyToNullStreamThenThrowsArgumentNullException()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms);
            Assert.ThrowsExactly<ArgumentNullException>(() => ds.CopyTo(null!));
        }

        #endregion CopyTo

        #region Stream Capabilities

        [TestMethod]
        public void WhenAccessCanReadThenReturnsTrue()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms);
            Assert.IsTrue(ds.CanRead);
        }

        [TestMethod]
        public void WhenAccessCanWriteThenReturnsFalse()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms);
            Assert.IsFalse(ds.CanWrite);
        }

        [TestMethod]
        public void WhenAccessCanSeekThenReturnsFalse()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms);
            Assert.IsFalse(ds.CanSeek);
        }

        #endregion Stream Capabilities

        #region Progress

        [TestMethod]
        public void WhenProgressBeforeReadThenReturnsZero()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms);
            Assert.AreEqual(0UL, ds.Progress);
        }

        [TestMethod]
        public void WhenProgressAfterFullReadThenReturnsPositiveValue()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms);
            using MemoryStream recoveryStream = new MemoryStream();
            ds.CopyTo(recoveryStream);
            Assert.IsTrue(ds.Progress > 0);
        }

        #endregion Progress

        #region NotSupported Operations

        [TestMethod]
        public void WhenWriteByteArrayThenThrowsNotSupportedException()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms);
            Assert.ThrowsExactly<NotSupportedException>(() => ds.Write(new byte[10], 0, 10));
        }

        [TestMethod]
        public void WhenWriteSpanThenThrowsNotSupportedException()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms);
            Assert.ThrowsExactly<NotSupportedException>(() => ds.Write(new byte[10].AsSpan()));
        }

        [TestMethod]
        public void WhenWriteByteThenThrowsNotSupportedException()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms);
            Assert.ThrowsExactly<NotSupportedException>(() => ds.WriteByte(0));
        }

        [TestMethod]
        public async Task WhenWriteAsyncByteArrayThenThrowsNotSupportedException()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms);
            await Assert.ThrowsExactlyAsync<NotSupportedException>(
                () => ds.WriteAsync(new byte[10], 0, 10));
        }

        [TestMethod]
        public void WhenWriteAsyncMemoryThenThrowsNotSupportedException()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms);
            Assert.ThrowsExactly<NotSupportedException>(
                () => ds.WriteAsync(new byte[10].AsMemory()));
        }

        [TestMethod]
        public void WhenReadByteThenReturnsDecompressedByte()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms);
            int b = ds.ReadByte();
            Assert.AreNotEqual(-1, b, "ReadByte should return a valid byte from a non-empty stream.");
        }

        [TestMethod]
        public void WhenSeekThenThrowsNotSupportedException()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms);
            Assert.ThrowsExactly<NotSupportedException>(() => ds.Seek(0, SeekOrigin.Begin));
        }

        [TestMethod]
        public void WhenSetLengthThenThrowsNotSupportedException()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms);
            Assert.ThrowsExactly<NotSupportedException>(() => ds.SetLength(0));
        }

        [TestMethod]
        public void WhenGetLengthThenThrowsNotSupportedException()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms);
            Assert.ThrowsExactly<NotSupportedException>(() => _ = ds.Length);
        }

        [TestMethod]
        public void WhenGetPositionThenThrowsNotSupportedException()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms);
            Assert.ThrowsExactly<NotSupportedException>(() => _ = ds.Position);
        }

        [TestMethod]
        public void WhenSetPositionThenThrowsNotSupportedException()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms);
            Assert.ThrowsExactly<NotSupportedException>(() => ds.Position = 0);
        }

        #endregion NotSupported Operations

        #region Constructor Guards

        [TestMethod]
        public void WhenConstructWithNullStreamThenThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new DecompressStream(null!));
        }

        #endregion Constructor Guards

        #region Dispose

        [TestMethod]
        public void WhenDisposeViaSyncThenCanReadReturnsFalse()
        {
            MemoryStream ms = MakeCompressedStream();
            DecompressStream ds = new DecompressStream(ms);
            ds.Dispose();
            Assert.IsFalse(ds.CanRead);
        }

        [TestMethod]
        public async Task WhenDisposeAsyncThenCanReadReturnsFalse()
        {
            MemoryStream ms = MakeCompressedStream();
            DecompressStream ds = new DecompressStream(ms);
            await ds.DisposeAsync();
            Assert.IsFalse(ds.CanRead);
        }

        [TestMethod]
        public void WhenReadAfterDisposeThenThrowsObjectDisposedException()
        {
            MemoryStream ms = MakeCompressedStream();
            DecompressStream ds = new DecompressStream(ms);
            ds.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(() => ds.Read(new byte[10], 0, 10));
        }

        [TestMethod]
        public void WhenCopyToAfterDisposeThenThrowsObjectDisposedException()
        {
            MemoryStream ms = MakeCompressedStream();
            DecompressStream ds = new DecompressStream(ms);
            ds.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(() => ds.CopyTo(new MemoryStream()));
        }

        [TestMethod]
        public void WhenFlushAfterDisposeThenThrowsObjectDisposedException()
        {
            MemoryStream ms = MakeCompressedStream();
            DecompressStream ds = new DecompressStream(ms);
            ds.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(() => ds.Flush());
        }

        [TestMethod]
        public void WhenProgressAfterDisposeThenThrowsObjectDisposedException()
        {
            MemoryStream ms = MakeCompressedStream();
            DecompressStream ds = new DecompressStream(ms);
            ds.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(() => _ = ds.Progress);
        }

        #endregion Dispose

        #region MT / ST

        [TestMethod]
        public void WhenReadWithMTStreamThenReturnsOriginalData()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms, nbThreads: 0);
            using MemoryStream recoveryStream = new MemoryStream();
            ds.CopyTo(recoveryStream);
            CollectionAssert.AreEqual(_originData, recoveryStream.ToArray());
        }

        [TestMethod]
        public void WhenReadWithSTStreamThenReturnsOriginalData()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms, nbThreads: 1);
            using MemoryStream recoveryStream = new MemoryStream();
            ds.CopyTo(recoveryStream);
            CollectionAssert.AreEqual(_originData, recoveryStream.ToArray());
        }

        #endregion MT / ST

        #region Small Input Buffer

        [TestMethod]
        public void WhenSmallInputBufferThenStillRecoversData()
        {
            using MemoryStream ms = MakeCompressedStream();
            using DecompressStream ds = new DecompressStream(ms, inBufferSize: 256);
            using MemoryStream recoveryStream = new MemoryStream();
            ds.CopyTo(recoveryStream);
            CollectionAssert.AreEqual(_originData, recoveryStream.ToArray());
        }

        #endregion Small Input Buffer

        #region FileStream Source

        [TestMethod]
        public void WhenReadFromFileStreamThenReturnsOriginalData()
        {
            string compressedPath = Path.Combine("Resources", $"dstream_test_{Path.GetRandomFileName()}.fl2");
            File.WriteAllBytes(compressedPath, _compressedData);
            try
            {
                using FileStream fs = new FileStream(compressedPath, FileMode.Open, FileAccess.Read);
                using DecompressStream ds = new DecompressStream(fs);
                using MemoryStream recoveryStream = new MemoryStream();
                ds.CopyTo(recoveryStream);
                CollectionAssert.AreEqual(_originData, recoveryStream.ToArray());
            }
            finally
            {
                File.Delete(compressedPath);
            }
        }

        #endregion FileStream Source
    }
}