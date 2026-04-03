using FastLZMA2Net;

namespace Test
{
    [TestClass]
    public class CompressStreamTests
    {
        private static byte[] _originData = null!;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            _originData = File.ReadAllBytes("Resources/dummy.raw");
        }

        #region Write One-shot

        [TestMethod]
        public void WhenWriteOneshotThenDecompressRecoversData()
        {
            using MemoryStream resultStream = new MemoryStream();
            using (CompressStream cs = new CompressStream(resultStream))
            {
                cs.Write(_originData);
            }

            byte[] compressed = resultStream.ToArray();
            byte[] recovered = FL2.Decompress(compressed);
            CollectionAssert.AreEqual(_originData, recovered);
        }

        [TestMethod]
        public void WhenWriteOneshotWithOffsetCountThenDecompressRecoversData()
        {
            using MemoryStream resultStream = new MemoryStream();
            using (CompressStream cs = new CompressStream(resultStream))
            {
                cs.Write(_originData, 0, _originData.Length);
            }

            byte[] compressed = resultStream.ToArray();
            byte[] recovered = FL2.Decompress(compressed);
            CollectionAssert.AreEqual(_originData, recovered);
        }

        #endregion Write One-shot

        #region Write Blocked / Append

        [TestMethod]
        public void WhenAppendBlockedThenDecompressRecoversData()
        {
            using MemoryStream resultStream = new MemoryStream();
            using (CompressStream cs = new CompressStream(resultStream))
            {
                int offset = 0;
                while (offset < _originData.Length)
                {
                    int remaining = _originData.Length - offset;
                    int bytesToWrite = Math.Min(1024, remaining);
                    cs.Append(_originData, offset, bytesToWrite);
                    offset += bytesToWrite;
                }
                cs.Flush();
            }

            byte[] compressed = resultStream.ToArray();
            byte[] recovered = FL2.Decompress(compressed);
            CollectionAssert.AreEqual(_originData, recovered);
        }

        [TestMethod]
        public void WhenAppendSpanThenDecompressRecoversData()
        {
            using MemoryStream resultStream = new MemoryStream();
            using (CompressStream cs = new CompressStream(resultStream))
            {
                int offset = 0;
                while (offset < _originData.Length)
                {
                    int remaining = _originData.Length - offset;
                    int bytesToWrite = Math.Min(4096, remaining);
                    cs.Append(_originData.AsSpan(offset, bytesToWrite));
                    offset += bytesToWrite;
                }
                cs.Flush();
            }

            byte[] compressed = resultStream.ToArray();
            byte[] recovered = FL2.Decompress(compressed);
            CollectionAssert.AreEqual(_originData, recovered);
        }

        #endregion Write Blocked / Append

        #region WriteAsync

        [TestMethod]
        public async Task WhenWriteAsyncByteArrayThenDecompressRecoversData()
        {
            using MemoryStream resultStream = new MemoryStream();
            using (CompressStream cs = new CompressStream(resultStream))
            {
                await cs.WriteAsync(_originData, 0, _originData.Length);
            }

            byte[] compressed = resultStream.ToArray();
            byte[] recovered = FL2.Decompress(compressed);
            CollectionAssert.AreEqual(_originData, recovered);
        }

        [TestMethod]
        public async Task WhenWriteAsyncMemoryThenDecompressRecoversData()
        {
            using MemoryStream resultStream = new MemoryStream();
            using (CompressStream cs = new CompressStream(resultStream))
            {
                await cs.WriteAsync(_originData.AsMemory());
            }

            byte[] compressed = resultStream.ToArray();
            byte[] recovered = FL2.Decompress(compressed);
            CollectionAssert.AreEqual(_originData, recovered);
        }

        #endregion WriteAsync

        #region Stream Capabilities

        [TestMethod]
        public void WhenAccessCanReadThenReturnsFalse()
        {
            using MemoryStream ms = new MemoryStream();
            using CompressStream cs = new CompressStream(ms);
            Assert.IsFalse(cs.CanRead);
        }

        [TestMethod]
        public void WhenAccessCanWriteThenReturnsTrue()
        {
            using MemoryStream ms = new MemoryStream();
            using CompressStream cs = new CompressStream(ms);
            Assert.IsTrue(cs.CanWrite);
        }

        [TestMethod]
        public void WhenAccessCanSeekThenReturnsFalse()
        {
            using MemoryStream ms = new MemoryStream();
            using CompressStream cs = new CompressStream(ms);
            Assert.IsFalse(cs.CanSeek);
        }

        #endregion Stream Capabilities

        #region NotSupported Operations

        [TestMethod]
        public void WhenReadThenThrowsNotSupportedException()
        {
            using MemoryStream ms = new MemoryStream();
            using CompressStream cs = new CompressStream(ms);
            Assert.ThrowsExactly<NotSupportedException>(() => cs.Read(new byte[1024], 0, 1024));
        }

        [TestMethod]
        public void WhenReadSpanThenThrowsNotSupportedException()
        {
            using MemoryStream ms = new MemoryStream();
            using CompressStream cs = new CompressStream(ms);
            Assert.ThrowsExactly<NotSupportedException>(() => cs.Read(new byte[1024].AsSpan()));
        }

        [TestMethod]
        public void WhenReadByteThenThrowsNotSupportedException()
        {
            using MemoryStream ms = new MemoryStream();
            using CompressStream cs = new CompressStream(ms);
            Assert.ThrowsExactly<NotSupportedException>(() => cs.ReadByte());
        }

        [TestMethod]
        public async Task WhenReadAsyncByteArrayThenThrowsNotSupportedException()
        {
            using MemoryStream ms = new MemoryStream();
            using CompressStream cs = new CompressStream(ms);
            await Assert.ThrowsExactlyAsync<NotSupportedException>(
                () => cs.ReadAsync(new byte[1024], 0, 1024));
        }

        [TestMethod]
        public void WhenReadAsyncMemoryThenThrowsNotSupportedException()
        {
            using MemoryStream ms = new MemoryStream();
            using CompressStream cs = new CompressStream(ms);
            Assert.ThrowsExactly<NotSupportedException>(
                () => cs.ReadAsync(new byte[1024].AsMemory()));
        }

        [TestMethod]
        public void WhenSeekThenThrowsNotSupportedException()
        {
            using MemoryStream ms = new MemoryStream();
            using CompressStream cs = new CompressStream(ms);
            Assert.ThrowsExactly<NotSupportedException>(() => cs.Seek(0, SeekOrigin.Begin));
        }

        [TestMethod]
        public void WhenSetLengthThenThrowsNotSupportedException()
        {
            using MemoryStream ms = new MemoryStream();
            using CompressStream cs = new CompressStream(ms);
            Assert.ThrowsExactly<NotSupportedException>(() => cs.SetLength(0));
        }

        [TestMethod]
        public void WhenGetLengthThenThrowsNotSupportedException()
        {
            using MemoryStream ms = new MemoryStream();
            using CompressStream cs = new CompressStream(ms);
            Assert.ThrowsExactly<NotSupportedException>(() => _ = cs.Length);
        }

        [TestMethod]
        public void WhenGetPositionThenThrowsNotSupportedException()
        {
            using MemoryStream ms = new MemoryStream();
            using CompressStream cs = new CompressStream(ms);
            Assert.ThrowsExactly<NotSupportedException>(() => _ = cs.Position);
        }

        [TestMethod]
        public void WhenSetPositionThenThrowsNotSupportedException()
        {
            using MemoryStream ms = new MemoryStream();
            using CompressStream cs = new CompressStream(ms);
            Assert.ThrowsExactly<NotSupportedException>(() => cs.Position = 0);
        }

        #endregion NotSupported Operations

        #region Parameters

        [TestMethod]
        public void WhenGetCompressLevelThenReturnsDefaultValue()
        {
            using MemoryStream ms = new MemoryStream();
            using CompressStream cs = new CompressStream(ms);
            Assert.IsTrue(cs.CompressLevel > 0);
        }

        [TestMethod]
        public void WhenGetDictionarySizeThenReturnsDefaultValue()
        {
            using MemoryStream ms = new MemoryStream();
            using CompressStream cs = new CompressStream(ms);
            Assert.IsTrue(cs.DictionarySize > 0);
        }

        [TestMethod]
        public void WhenGetSearchDepthThenReturnsDefaultValue()
        {
            using MemoryStream ms = new MemoryStream();
            using CompressStream cs = new CompressStream(ms);
            Assert.IsTrue(cs.SearchDepth > 0);
        }

        [TestMethod]
        public void WhenGetFastLengthThenReturnsDefaultValue()
        {
            using MemoryStream ms = new MemoryStream();
            using CompressStream cs = new CompressStream(ms);
            Assert.IsTrue(cs.FastLength > 0);
        }

        [TestMethod]
        public void WhenGetHighCompressLevelThenDoesNotThrow()
        {
            using MemoryStream ms = new MemoryStream();
            using CompressStream cs = new CompressStream(ms);
            _ = cs.HighCompressLevel;
        }

        #endregion Parameters

        #region Constructor Guards

        [TestMethod]
        public void WhenConstructWithNullStreamThenThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new CompressStream(null!));
        }

        [TestMethod]
        public void WhenConstructWithZeroOutBufferSizeThenThrowsArgumentOutOfRangeException()
        {
            using MemoryStream ms = new MemoryStream();
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new CompressStream(ms, outBufferSize: 0));
        }

        [TestMethod]
        public void WhenConstructWithNegativeOutBufferSizeThenThrowsArgumentOutOfRangeException()
        {
            using MemoryStream ms = new MemoryStream();
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new CompressStream(ms, outBufferSize: -1));
        }

        [TestMethod]
        public void WhenConstructWithOversizedOutBufferSizeThenThrowsArgumentOutOfRangeException()
        {
            using MemoryStream ms = new MemoryStream();
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => new CompressStream(ms, outBufferSize: (nint)Array.MaxLength + 1));
        }

        #endregion Constructor Guards

        #region Dispose

        [TestMethod]
        public void WhenDisposeViaSyncThenCanWriteReturnsFalse()
        {
            MemoryStream ms = new MemoryStream();
            CompressStream cs = new CompressStream(ms);
            cs.Dispose();
            Assert.IsFalse(cs.CanWrite);
        }

        [TestMethod]
        public async Task WhenDisposeAsyncThenCanWriteReturnsFalse()
        {
            MemoryStream ms = new MemoryStream();
            CompressStream cs = new CompressStream(ms);
            await cs.DisposeAsync();
            Assert.IsFalse(cs.CanWrite);
        }

        [TestMethod]
        public void WhenWriteAfterDisposeThenThrowsObjectDisposedException()
        {
            MemoryStream ms = new MemoryStream();
            CompressStream cs = new CompressStream(ms);
            cs.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(() => cs.Write(new byte[10]));
        }

        [TestMethod]
        public void WhenAppendAfterDisposeThenThrowsObjectDisposedException()
        {
            MemoryStream ms = new MemoryStream();
            CompressStream cs = new CompressStream(ms);
            cs.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(() => cs.Append(new byte[10], 0, 10));
        }

        [TestMethod]
        public void WhenAppendSpanAfterDisposeThenThrowsObjectDisposedException()
        {
            MemoryStream ms = new MemoryStream();
            CompressStream cs = new CompressStream(ms);
            cs.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(() => cs.Append(new byte[10].AsSpan()));
        }

        [TestMethod]
        public async Task WhenWriteAsyncByteArrayAfterDisposeThenThrowsObjectDisposedException()
        {
            MemoryStream ms = new MemoryStream();
            CompressStream cs = new CompressStream(ms);
            cs.Dispose();
            await Assert.ThrowsExactlyAsync<ObjectDisposedException>(
                () => cs.WriteAsync(new byte[10], 0, 10));
        }

        [TestMethod]
        public async Task WhenWriteAsyncMemoryAfterDisposeThenThrowsObjectDisposedException()
        {
            MemoryStream ms = new MemoryStream();
            CompressStream cs = new CompressStream(ms);
            cs.Dispose();
            await Assert.ThrowsExactlyAsync<ObjectDisposedException>(
                () => cs.WriteAsync(new byte[10].AsMemory()).AsTask());
        }

        [TestMethod]
        public void WhenDisposeTwiceThenDoesNotThrow()
        {
            MemoryStream ms = new MemoryStream();
            CompressStream cs = new CompressStream(ms);
            cs.Dispose();
            cs.Dispose();
        }

        [TestMethod]
        public async Task WhenDisposeAsyncTwiceThenDoesNotThrow()
        {
            MemoryStream ms = new MemoryStream();
            CompressStream cs = new CompressStream(ms);
            await cs.DisposeAsync();
            await cs.DisposeAsync();
        }

        [TestMethod]
        public void WhenFlushAfterDisposeThenThrowsObjectDisposedException()
        {
            MemoryStream ms = new MemoryStream();
            CompressStream cs = new CompressStream(ms);
            cs.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(() => cs.Flush());
        }

        [TestMethod]
        public void WhenSetParameterAfterDisposeThenThrowsObjectDisposedException()
        {
            MemoryStream ms = new MemoryStream();
            CompressStream cs = new CompressStream(ms);
            cs.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(
                () => cs.SetParameter(FL2Parameter.CompressionLevel, 5));
        }

        [TestMethod]
        public void WhenGetParameterAfterDisposeThenThrowsObjectDisposedException()
        {
            MemoryStream ms = new MemoryStream();
            CompressStream cs = new CompressStream(ms);
            cs.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(
                () => cs.GetParameter(FL2Parameter.CompressionLevel));
        }

        #endregion Dispose

        #region MT

        [TestMethod]
        public void WhenWriteWithMTStreamThenDecompressRecoversData()
        {
            using MemoryStream resultStream = new MemoryStream();
            using (CompressStream cs = new CompressStream(resultStream, nbThreads: 0))
            {
                cs.Write(_originData);
            }

            byte[] compressed = resultStream.ToArray();
            byte[] recovered = FL2.Decompress(compressed);
            CollectionAssert.AreEqual(_originData, recovered);
        }

        #endregion MT
    }
}