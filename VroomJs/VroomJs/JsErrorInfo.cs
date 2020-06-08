using System;
using System.Runtime.InteropServices;

namespace VroomJs
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct JsErrorInfo
    {
        public JsValue Error;
        public int Line;
        public int Column;
        public IntPtr Resource;
        public IntPtr Description;
        public IntPtr Type;
        public IntPtr Text;
        public IntPtr StackStr;
        public IntPtr StackFrames; // pointer to a JsStackFrame
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct JsStackFrame
    {
        public int Line;
        public int Column;
        public IntPtr Resource;
        public IntPtr Function;
        public IntPtr Next; // pointer to next JsStackFrame
    }
}
