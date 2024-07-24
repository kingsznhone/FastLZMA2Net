using System.Runtime.InteropServices;

namespace FastLZMA2Net
{
    public enum FL2Strategy
    {
        Fast,
        Opt,
        Ultra
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct CompressionParameters
    {
        [FieldOffset(0)]
        public nuint DictionarySize;  //Only for x64

        [FieldOffset(8)]
        public uint OverlapFraction;

        [FieldOffset(12)]
        public uint ChainLog;

        [FieldOffset(16)]
        public uint CyclesLog;

        [FieldOffset(20)]
        public uint SearchDepth;

        [FieldOffset(24)]
        public uint FastLength;

        [FieldOffset(28)]
        public uint DivideAndConquer;

        [FieldOffset(32)]
        public FL2Strategy Strategy;
    }
}