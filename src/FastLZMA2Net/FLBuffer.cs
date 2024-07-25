using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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

}
