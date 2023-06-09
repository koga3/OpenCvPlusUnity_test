using System;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

#pragma warning disable 1591

namespace OpenCvSharp
{

    static partial class NativeMethods
    {
        [Pure, DllImport(DllExternQR, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern IntPtr string_new1();

        [Pure, DllImport(DllExternQR, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true, CharSet = CharSet.Ansi)]
        public static extern IntPtr string_new2([MarshalAs(UnmanagedType.LPArray)] byte[] str);

        [DllImport(DllExternQR, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void string_delete(IntPtr s);

        [Pure, DllImport(DllExternQR, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern unsafe sbyte* string_c_str(IntPtr s);

        [Pure, DllImport(DllExternQR, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint string_size(IntPtr s);
    }
}