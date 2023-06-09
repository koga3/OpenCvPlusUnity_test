﻿using System;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

#pragma warning disable 1591
#pragma warning disable CA1401 // P/Invokes should not be visible
#pragma warning disable IDE1006 // Naming style
// ReSharper disable InconsistentNaming

namespace OpenCvSharp
{
    static partial class NativeMethods
    {
#if !UNITY_EDITOR && UNITY_IOS
		public const string DllExternQR = "__Internal";
#elif UNITY_EDITOR
        public const string DllExternQR = "OpenCvSharpExternQRdetect";
#else
        public const string DllExternQR = "OpenCvSharpExtern";
#endif

        [Pure, DllImport(DllExternQR, CallingConvention = CallingConvention.Cdecl, BestFitMapping = false, ThrowOnUnmappableChar = true, ExactSpelling = true)]
        public static extern ExceptionStatus objdetect_QRCodeDetector_new(out IntPtr returnValue);

        [Pure, DllImport(DllExternQR, CallingConvention = CallingConvention.Cdecl, BestFitMapping = false, ThrowOnUnmappableChar = true, ExactSpelling = true)]
        public static extern ExceptionStatus objdetect_QRCodeDetector_delete(IntPtr obj);

        [Pure, DllImport(DllExternQR, CallingConvention = CallingConvention.Cdecl, BestFitMapping = false, ThrowOnUnmappableChar = true, ExactSpelling = true)]
        public static extern ExceptionStatus objdetect_QRCodeDetector_setEpsX(IntPtr obj, double epsX);

        [Pure, DllImport(DllExternQR, CallingConvention = CallingConvention.Cdecl, BestFitMapping = false, ThrowOnUnmappableChar = true, ExactSpelling = true)]
        public static extern ExceptionStatus objdetect_QRCodeDetector_setEpsY(IntPtr obj, double epsY);

        [Pure, DllImport(DllExternQR, CallingConvention = CallingConvention.Cdecl, BestFitMapping = false, ThrowOnUnmappableChar = true, ExactSpelling = true)]
        public static extern ExceptionStatus objdetect_QRCodeDetector_detect(IntPtr obj, IntPtr img, IntPtr points, out int returnValue);

        [Pure, DllImport(DllExternQR, CallingConvention = CallingConvention.Cdecl, BestFitMapping = false, ThrowOnUnmappableChar = true, ExactSpelling = true)]
        public static extern ExceptionStatus objdetect_QRCodeDetector_decode(
            IntPtr obj, IntPtr img, IntPtr points, IntPtr straightQrCode, IntPtr returnValue);

        [Pure, DllImport(DllExternQR, CallingConvention = CallingConvention.Cdecl, BestFitMapping = false, ThrowOnUnmappableChar = true, ExactSpelling = true)]
        public static extern ExceptionStatus objdetect_QRCodeDetector_detectAndDecode(
            IntPtr obj, IntPtr img, IntPtr points,
            IntPtr straightQrCode, IntPtr returnValue);

        [Pure, DllImport(DllExternQR, CallingConvention = CallingConvention.Cdecl, BestFitMapping = false, ThrowOnUnmappableChar = true, ExactSpelling = true)]
        public static extern ExceptionStatus objdetect_QRCodeDetector_detectMulti(IntPtr obj, IntPtr img, IntPtr points, out int returnValue);

        [Pure, DllImport(DllExternQR, CallingConvention = CallingConvention.Cdecl, BestFitMapping = false, ThrowOnUnmappableChar = true, ExactSpelling = true)]
        public static extern ExceptionStatus objdetect_QRCodeDetector_decodeMulti(
            IntPtr obj, IntPtr img, IntPtr points, IntPtr decodedInfo, IntPtr straightQrCode, out int returnValue);

        [Pure, DllImport(DllExternQR, CallingConvention = CallingConvention.Cdecl, BestFitMapping = false, ThrowOnUnmappableChar = true, ExactSpelling = true)]
        public static extern ExceptionStatus objdetect_QRCodeDetector_decodeMulti_NoStraightQrCode(
            IntPtr obj, IntPtr img, IntPtr points, IntPtr decodedInfo, out int returnValue);
    }
}