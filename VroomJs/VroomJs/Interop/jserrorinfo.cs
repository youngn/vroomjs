using System;
using System.Runtime.InteropServices;

namespace VroomJs.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct jserrorinfo
    {
        public jsvalue Error;
        public int Line;
        public int Column;
        public IntPtr Resource;
        public IntPtr Type;
        public IntPtr Text;
        public IntPtr StackStr;
        public IntPtr StackFrames; // pointer to a jsstackframe
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct jsstackframe
    {
        public int Line;
        public int Column;
        public IntPtr Resource;
        public IntPtr Function;
        public IntPtr Next; // pointer to next jsstackframe
    }
}
