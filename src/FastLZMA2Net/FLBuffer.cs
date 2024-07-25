using System.Runtime.InteropServices;

namespace FastLZMA2Net
{
    public struct FL2InBuffer
    {
        public nint src;
        public nuint size;
        public nuint pos;
    }

    //[CustomMarshaller(typeof(FL2InBuffer), MarshalMode.ManagedToUnmanagedIn, typeof(FL2InBufferMarshaller))]
    //public static unsafe class FL2InBufferMarshaller
    //{
    //    public struct FL2InBufferUnmanaged
    //    {
    //        public nint src;
    //        public nuint size;
    //        public nuint pos;
    //    }
    //    public static FL2InBufferUnmanaged ConvertToUnmanaged(FL2InBuffer managed)
    //    {
    //        managed.nativeHandle = GCHandle.Alloc(managed.src);
    //        var unmanaged = new FL2InBufferUnmanaged()
    //        {
    //            src =Marshal.UnsafeAddrOfPinnedArrayElement(managed.src,0),
    //            size =managed.size,
    //            pos = managed.size,
    //        };
    //        return unmanaged;
    //    }

    //    public static void Free(FL2InBufferUnmanaged unmanaged)
    //    {
    //    }
    //}

    public struct FL2OutBuffer
    {
        public nint dst;
        public nuint size;
        public nuint pos;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FL2DictBuffer
    {
        [MarshalAs(UnmanagedType.LPArray)]
        public byte[] dst;

        public nuint size;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FL2cBuffer
    {
        [MarshalAs(UnmanagedType.LPArray)]
        public byte[] src;

        public nuint size;
    }
}