namespace FastLZMA2Net
{
    public struct FL2InBuffer
    {
        /// <summary>
        /// start of input buffer
        /// </summary>
        public nint src;

        /// <summary>
        /// size of input buffer
        /// </summary>
        public nuint size;

        /// <summary>
        /// position where reading stopped. Will be updated. Necessarily 0 <= pos <= size
        /// </summary>
        public nuint pos;
    }

    public struct FL2OutBuffer
    {
        /// <summary>
        /// start of output buffer
        /// </summary>
        public nint dst;

        /// <summary>
        /// size of output buffer
        /// </summary>
        public nuint size;

        /// <summary>
        /// position where writing stopped. Will be updated. Necessarily 0 <= pos <= size
        /// </summary>
        public nuint pos;
    }

    public struct FL2DictBuffer
    {
        /// <summary>
        /// start of available dict buffer
        /// </summary>
        public nint dst;

        /// <summary>
        /// size of dict remaining
        /// </summary>
        public nuint size;
    }

    public struct FL2cBuffer
    {
        /// <summary>
        /// start of compressed data
        /// </summary>
        public nint src;

        /// <summary>
        /// size of compressed data
        /// </summary>
        public nuint size;
    }
}