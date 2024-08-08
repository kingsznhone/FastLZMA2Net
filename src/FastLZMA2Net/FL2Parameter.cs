namespace FastLZMA2Net
{
    public enum FL2Parameter
    {
        /// <summary>
        /// Update all compression parameters according to pre-defined cLevel table
        /// Default level is FL2_CLEVEL_DEFAULT==6.
        /// Setting FL2_p_highCompression to 1 switches to an alternate cLevel table.
        /// </summary>
        CompressionLevel,

        /// <summary>
        /// Maximize compression ratio for a given dictionary size.Levels 1..10 = dictionaryLog 20..29 (1 Mb..512 Mb).
        /// Typically provides a poor speed/ratio tradeoff.
        /// </summary>
        HighCompression,

        /// <summary>
        /// Maximum allowed back-reference distance, expressed as power of 2.
        /// Must be clamped between FL2_DICTLOG_MIN and FL2_DICTLOG_MAX.
        /// Default = 24
        /// </summary>
        DictionaryLog,

        /// <summary>
        /// Same as above but expressed as an absolute value.
        /// Must be clamped between FL2_DICTSIZE_MIN and FL2_DICTSIZE_MAX.
        /// Default = 16 Mb
        /// </summary>
        DictionarySize,

        /// <summary>
        /// The radix match finder is block-based, so some overlap is retained from each block to improve compression of the next.
        /// This value is expressed as n / 16 of the block size (dictionary size). Larger values are slower.
        /// Values above 2 mostly yield only a small improvement in compression.
        /// A large value for a small dictionary may worsen multithreaded compression.
        /// Default = 2
        /// </summary>
        OverlapFraction,

        /// <summary>
        /// For multithreaded decompression. A dictionary reset will occur
        /// after each dictionarySize * resetInterval bytes of input.
        /// Default = 4
        /// </summary>
        ResetInterval,

        /// <summary>
        /// Buffering speeds up the matchfinder. Buffer resize determines the percentage of
        /// the normal buffer size used, which depends on dictionary size.
        /// 0=50, 1=75, 2=100, 3=150, 4=200. Higher number = slower, better
        /// compression, higher memory usage. A CPU with a large memory cache
        /// may make effective use of a larger buffer.
        /// Default = 2
        /// </summary>
        BufferResize,

        /// <summary>
        /// Size of the hybrid mode HC3 hash chain, as a power of 2.
        /// Resulting table size is (1 << (chainLog+2)) bytes.
        /// Larger tables result in better and slower compression.
        /// This parameter is only used by the hybrid "ultra" strategy.
        /// Default = 9
        /// </summary>
        HybridChainLog,

        /// <summary>
        /// Number of search attempts made by the HC3 match finder.
        /// Used only by the hybrid "ultra" strategy.
        /// More attempts result in slightly better and slower compression.
        /// Default = 1
        /// </summary>
        HybridCycles,

        /// <summary>
        /// Match finder will resolve string matches up to this length.
        /// If a longer match exists further back in the input, it will not be found.
        /// Default = 42
        /// </summary>
        SearchDepth,

        /// <summary>
        /// Only useful for strategies >= opt.
        /// Length of match considered "good enough" to stop search.
        /// Larger values make compression stronger and slower.
        /// Default = 48
        /// </summary>
        FastLength,

        /// <summary>
        /// Split long chains of 2-byte matches into shorter chains with a small overlap for further processing.
        /// Allows buffering of all chains at length 2.
        /// Faster, less compression. Generally a good tradeoff.
        /// Default = enabled
        /// </summary>
        DivideAndConquer,

        /// <summary>
        /// 1 = fast; 2 = optimized, 3 = ultra (hybrid mode).
        /// The higher the value of the selected strategy, the more complex it is,
        /// resulting in stronger and slower compression.
        /// Default = ultra
        /// </summary>
        Strategy,

        /// <summary>
        /// lc value for LZMA2 encoder
        /// Default = 3
        /// </summary>
        LiteralCtxBits,

        /// <summary>
        /// lp value for LZMA2 encoder
        /// Default = 0
        /// </summary>
        LiteralPosBits,

        /// <summary>
        /// pb value for LZMA2 encoder
        /// Default = 2
        /// </summary>
        posBits,

        /// <summary>
        /// Omit the property byte at the start of the stream. For use within 7-zip
        /// or other containers which store the property byte elsewhere.
        /// A stream compressed under this setting cannot be decoded by this library.
        /// </summary>
        Properties,

        /// <summary>
        /// Calculate a 32-bit xxhash value from the input data and store it
        /// after the stream terminator. The value will be checked on decompression.
        /// 0 = do not calculate; 1 = calculate (default)
        /// </summary>
        DoXXHash,

        /// <summary>
        /// Use the reference matchfinder for development purposes. SLOW.
        /// </summary>
        UseReferenceMF
    }
}