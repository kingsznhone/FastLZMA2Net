﻿using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FastLZMA2Net
{
    //FL2CompressContext
    public partial class Compressor : IDisposable
    {

        private readonly nint _context;
        private bool disposed = false;
        private bool disposedValue;

        public uint ThreadCount => ExternMethods.FL2_getCCtxThreadCount(_context);
        public byte DictSizeProperty => ExternMethods.FL2_getCCtxDictProp(_context);

        public int CompressLevel
        {
            get => (int)GetParameter(CompressParameterEnum.FL2_p_compressionLevel);
            set => SetParameter(CompressParameterEnum.FL2_p_compressionLevel, (nuint)value);
        }

        public int HighCompressLevel
        {
            get => (int)GetParameter(CompressParameterEnum.FL2_p_highCompression);
            set => SetParameter(CompressParameterEnum.FL2_p_highCompression, (nuint)value);
        }

        public int DictionarySize
        {
            get => (int)GetParameter(CompressParameterEnum.FL2_p_dictionarySize);
            set => SetParameter(CompressParameterEnum.FL2_p_dictionarySize, (nuint)value);
        }

        public int SearchDepth
        {
            get => (int)GetParameter(CompressParameterEnum.FL2_p_searchDepth);
            set => SetParameter(CompressParameterEnum.FL2_p_searchDepth, (nuint)value);
        }
        public int FastLength
        {
            get => (int)GetParameter(CompressParameterEnum.FL2_p_fastLength);
            set => SetParameter(CompressParameterEnum.FL2_p_fastLength, (nuint)value);
        }


        public Compressor()
        {
            _context = ExternMethods.FL2_createCCtx();
        }

        public Compressor(uint nThread)
        {
            _context = ExternMethods.FL2_createCCtxMt(nThread);
        }


        public byte[] Compress(byte[] data)
        {
            return Compress(data, CompressLevel);
        }

        public byte[] Compress(byte[] data, int Level)
        {
            byte[] buffer = new byte[FL2.FindCompressBound(data)];
            nuint code = ExternMethods.FL2_compressCCtx(_context, buffer, (nuint)buffer.Length, data, (nuint)data.Length, Level);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return buffer[..(int)code];
        }

        public void SetParameter(CompressParameterEnum param, nuint value)
        {
            var code = ExternMethods.FL2_CCtx_setParameter(_context, param, value);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
        }

        public nuint GetParameter(CompressParameterEnum param)
        {
            var code = ExternMethods.FL2_CCtx_getParameter(_context, param);
            if (FL2Exception.IsError(code))
            {
                throw new FL2Exception(code);
            }
            return code;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing){ }
                ExternMethods.FL2_freeCCtx(_context);
                disposedValue = true;
            }
        }

        ~Compressor()
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