using System.Runtime.InteropServices;

namespace VroomJs
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct JsErrorInfo
    {
        public int Line;
        public int Column;
        public JsValue Resource;
        public JsValue Message;
        public JsValue Error;
        public JsValue Type;
    }
}
