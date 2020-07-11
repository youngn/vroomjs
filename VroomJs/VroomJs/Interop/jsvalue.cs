using System;
using System.Runtime.InteropServices;

namespace VroomJs.Interop
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct jsvalue
    {
        [FieldOffset(0)] public int I32;
        [FieldOffset(0)] public long I64;
        [FieldOffset(0)] public double Num;
        [FieldOffset(0)] public IntPtr Ptr;

        [FieldOffset(8)] public JsValueType Type; // marshaled as integer.

        [FieldOffset(12)] public int Length; // Length of array or string
        [FieldOffset(12)] public int TemplateId;
    }
}
