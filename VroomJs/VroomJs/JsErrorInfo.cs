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
        public IntPtr Message;
        public IntPtr Type;
    }
}
