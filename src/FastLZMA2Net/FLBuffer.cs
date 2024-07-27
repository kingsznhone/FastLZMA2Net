using System.Runtime.InteropServices;

namespace FastLZMA2Net
{
    public struct FL2InBuffer
    {
        public nint src;
        public nuint size;
        public nuint pos;
    }
    public struct FL2OutBuffer
    {
        public nint dst;
        public nuint size;
        public nuint pos;
    }

    public struct FL2DictBuffer
    {
        public nint dst;
        public nuint size;
    }

    public struct FL2cBuffer
    {
        public nint src;
        public nuint size;
    }
}