using System.Runtime.InteropServices;

namespace VroomJs.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct allocationstats
    {
        public int EngineCount;
        public int ContextCount;
        public int ScriptCount;
        public int HostObjectCount;
        public int JsObjectCount;
    }
}
