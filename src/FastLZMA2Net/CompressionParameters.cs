namespace FastLZMA2Net
{
    /// <summary>
    /// Encoder strategy used to balance compression speed and ratio.
    /// </summary>
    public enum FL2Strategy
    {
        /// <summary>
        /// Fastest strategy with the lowest compression ratio.
        /// </summary>
        Fast,

        /// <summary>
        /// Balanced optimized strategy.
        /// </summary>
        Opt,

        /// <summary>
        /// Highest compression ratio using hybrid mode.
        /// </summary>
        Ultra
    }

    /// <summary>
    /// Compression settings used to configure the native encoder.
    /// </summary>
    public struct CompressionParameters : IEquatable<CompressionParameters>
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

        /// <summary>
        /// Indicates whether this instance has the same parameter values as another instance.
        /// </summary>
        /// <param name="other">The value to compare against.</param>
        /// <returns><see langword="true" /> when all fields match; otherwise, <see langword="false" />.</returns>
        public bool Equals(CompressionParameters other)
        {
            return DictionarySize == other.DictionarySize
                && OverlapFraction == other.OverlapFraction
                && ChainLog == other.ChainLog
                && CyclesLog == other.CyclesLog
                && SearchDepth == other.SearchDepth
                && FastLength == other.FastLength
                && DivideAndConquer == other.DivideAndConquer
                && Strategy == other.Strategy;
        }

        /// <summary>
        /// Determines whether the specified object is equal to this instance.
        /// </summary>
        public override bool Equals(object? obj)
        {
            return obj is CompressionParameters other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(
                DictionarySize,
                OverlapFraction,
                ChainLog,
                CyclesLog,
                SearchDepth,
                FastLength,
                DivideAndConquer,
                Strategy);
        }

        /// <summary>
        /// Compares two parameter sets for value equality.
        /// </summary>
        public static bool operator ==(CompressionParameters left, CompressionParameters right)
            => left.Equals(right);

        /// <summary>
        /// Compares two parameter sets for value inequality.
        /// </summary>
        public static bool operator !=(CompressionParameters left, CompressionParameters right)
            => !(left == right);
    }
}