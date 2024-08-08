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
        /// <summary>
        /// largest match distance : larger == more compression, more memory needed during decompression;> 64Mb == more memory per byte, slower
        /// </summary>
        public nuint DictionarySize;
        /// <summary>
        /// overlap between consecutive blocks in 1/16 units: larger == more compression, slower
        /// </summary>
        public uint OverlapFraction;
        /// <summary>
        /// HC3 sliding window : larger == more compression, slower; hybrid mode only (ultra)
        /// </summary>
        public uint ChainLog;
        /// <summary>
        /// nb of searches : larger == more compression, slower; hybrid mode only (ultra)
        /// </summary>
        public uint CyclesLog;
        /// <summary>
        /// maximum depth for resolving string matches : larger == more compression, slower
        /// </summary>
        public uint SearchDepth;
        /// <summary>
        /// acceptable match size for parser : larger == more compression, slower; fast bytes parameter from 7-Zip
        /// </summary>
        public uint FastLength;
        /// <summary>
        /// split long chains of 2-byte matches into shorter chains with a small overlap : faster, somewhat less compression; enabled by default
        /// </summary>
        public uint DivideAndConquer;
        /// <summary>
        /// encoder strategy : fast, optimized or ultra (hybrid)
        /// </summary>
        public FL2Strategy Strategy;
    }
}