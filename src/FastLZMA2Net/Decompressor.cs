﻿namespace FastLZMA2Net
{
    /// <summary>
    /// Fast LZMA2 Decompress Context
    /// </summary>
    public partial class Decompressor : IDisposable
    {
        private readonly nint _context;
        public nint ContextPtr => _context;
        private bool disposedValue;

        /// <summary>
        /// Thread use of the context
        /// </summary>
        public uint ThreadCount => NativeMethods.FL2_getDCtxThreadCount(_context);

        /// <summary>
        /// Initialize new decompress context
        /// </summary>
        /// <param name="nbThreads"></param>
        public Decompressor(uint nbThreads = 0)
        {
            if (nbThreads == 1)
            {
                _context = NativeMethods.FL2_createDCtx();
            }
            else
            {
                _context = NativeMethods.FL2_createDCtxMt(nbThreads);
            }
        }

        /// <summary>
        /// Initial new context with specific dict size property
        /// </summary>
        /// <param name="prop">dictSizeProperty</param>
        public void Init(byte prop)
        {
            NativeMethods.FL2_initDCtx(_context, prop);
        }

        /// <summary>
        /// Decompress
        /// </summary>
        /// <param name="data">Fast LZMA2 data</param>
        /// <returns>Raw data</returns>
        /// <exception cref="FL2Exception"></exception>
        public byte[] Decompress(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);
            nuint decompressedSize = FL2.FindDecompressedSize(data);
            byte[] decompressed = new byte[decompressedSize];
            nuint code = NativeMethods.FL2_decompressDCtx(_context, decompressed, decompressedSize, data, (nuint)data.Length);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return decompressed[0..(int)code];
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing) { }
                NativeMethods.FL2_freeDCtx(_context);
                disposedValue = true;
            }
        }

        ~Decompressor()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}