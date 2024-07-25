using System.Runtime.InteropServices;

namespace FastLZMA2Net
{
    public enum FL2Strategy
    {
        Fast,
        Opt,
        Ultra
    }

    public struct CompressionParameters
    {
        public nuint DictionarySize;  //Only for x64

        public uint OverlapFraction;

        public uint ChainLog;

        public uint CyclesLog;

        public uint SearchDepth;

        public uint FastLength;

        public uint DivideAndConquer;

        public FL2Strategy Strategy;
    }
}