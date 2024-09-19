using FastLZMA2Net;

namespace Test
{
    [TestClass]
    public class DStreamTest
    {
        public DStreamTest()
        {
            File.WriteAllBytes(@"Resources/dummy.fl2", FL2.CompressMT(File.ReadAllBytes(@"Resources/dummy.raw"), 9, 0));
        }

        [TestMethod]
        public void TestBlocked()
        {
            //Use Dummy File
            byte[] origin = File.ReadAllBytes(@"Resources/dummy.raw");
            byte[] compressed = FL2.CompressMT(origin, 5, 0);

            byte[] buffer = new byte[1024];
            using (MemoryStream recoveryStream = new MemoryStream())
            {
                using (MemoryStream ms = new MemoryStream(compressed))
                {
                    using (DecompressStream ds = new DecompressStream(ms, nbThreads: 0, inBufferSize: 1024))
                    {
                        int reads = 0;
                        while ((reads = ds.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            recoveryStream.Write(buffer, 0, reads);
                        }
                    }
                }
                byte[] recovery = recoveryStream.ToArray();
                Assert.IsTrue(origin.SequenceEqual(recovery));
            }
        }

        [TestMethod]
        public async Task TestAsync()
        {
            byte[] origin = File.ReadAllBytes(@"Resources/dummy.raw");
            byte[] compressed = FL2.CompressMT(origin, 10, 1);
            byte[] buffer = new byte[64*1024*1024];
            Memory<byte> memory = new Memory<byte>(buffer);
            int bytesRead = 0;
            using (MemoryStream ms = new MemoryStream(compressed))
            {
                using (DecompressStream ds = new DecompressStream(ms, nbThreads: 0, inBufferSize: 1024))
                {
                    bytesRead =  await ds.ReadAsync(memory);
                }
            }
            byte[] recovered = buffer[0..bytesRead];
            Assert.IsTrue(origin.SequenceEqual(recovered));
        }

        [TestMethod]
        public async Task TestBlockedAsync()
        {
            //Use Dummy File
            byte[] origin = File.ReadAllBytes(@"Resources/dummy.raw");
            byte[] compressed = FL2.CompressMT(origin, 1, 0);

            //Test Block buffer
            byte[] buffer = new byte[1024];
            using (MemoryStream recoveryStream = new MemoryStream())
            {
                using (MemoryStream ms = new MemoryStream(compressed))
                {
                    using (DecompressStream ds = new DecompressStream(ms, nbThreads: 0, inBufferSize: 256))
                    {
                        int reads = 0;
                        while ((reads = await ds.ReadAsync(buffer, 0, buffer.Length)) != 0)
                        {
                            recoveryStream.Write(buffer, 0, reads);
                        }
                    }
                }
                Assert.IsTrue(origin.SequenceEqual(recoveryStream.ToArray()));
            }
        }

        [TestMethod]
        public async Task TestCopyTo()
        {
            //Use Dummy File
            byte[] origin = File.ReadAllBytes(@"Resources/dummy.raw");
            byte[] compressed = FL2.CompressMT(origin, 1, 0);

            byte[] buffer = new byte[1024];
            using (MemoryStream recoveryStream = new MemoryStream())
            {
                using (MemoryStream ms = new MemoryStream(compressed))
                {
                    using (DecompressStream ds = new DecompressStream(ms, nbThreads: 0, inBufferSize: 1024))
                    {
                        ds.CopyTo(recoveryStream);
                    }
                }
                Assert.IsTrue(origin.SequenceEqual(recoveryStream.ToArray()));
            }
        }

        [TestMethod]
        public void TestDFA()
        {
            //Use Dummy file
            byte[] origin = File.ReadAllBytes(@"Resources/dummy.raw");
            byte[] compressed = FL2.CompressMT(origin, 0, 0);
            File.WriteAllBytes(@"Resources/dummy.fl2", compressed);
            //byte[] origin = File.ReadAllBytes(@"Resources/dummy.raw");
            //byte[] compressed = FL2.CompressMT(origin, 1, 0);

            //test direct file read
            using (MemoryStream recoveryStream = new MemoryStream())
            {
                using (FileStream fs = new FileStream(@"Resources/dummy.fl2", FileMode.Open, FileAccess.Read))
                {
                    using (DecompressStream ds = new DecompressStream(fs, nbThreads: 0))
                    {
                        ds.CopyTo(recoveryStream);
                    }
                }
                Assert.IsTrue(origin.SequenceEqual(recoveryStream.ToArray()));
            }

            //test direct file write
            if (File.Exists(@"Resources/recovery.raw")) { File.Delete(@"Resources/recovery.raw"); }
            using (FileStream recoveryStream = new FileStream(@"Resources/recovery.raw", FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (FileStream fs = new FileStream(@"Resources/dummy.fl2", FileMode.Open, FileAccess.Read))
                {
                    using (DecompressStream ds = new DecompressStream(fs, nbThreads: 0))
                    {
                        ds.CopyTo(recoveryStream);
                    }
                }
            }
            Assert.IsTrue(origin.SequenceEqual(File.ReadAllBytes(@"Resources/recovery.raw")));
        }
    }
}