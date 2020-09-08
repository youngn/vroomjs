using System;
using System.Runtime.InteropServices;
using VroomJs.Interop;

namespace VroomJs
{
    internal abstract class V8DisposableSafeHandle : SafeHandle
    {
        protected V8DisposableSafeHandle(IntPtr invalidHandleValue, bool ownsHandle)
            : base(invalidHandleValue, ownsHandle)
        {
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            NativeApi.js_dispose(handle);
            return true;
        }
    }

    internal sealed class EngineHandle : V8DisposableSafeHandle
    {
        // Called by P/Invoke when returning SafeHandles
        private EngineHandle()
            : base(IntPtr.Zero, true)
        {
        }

        public static EngineHandle CreateInvalid()
        {
            return new EngineHandle();
        }
    }

    internal sealed class ContextHandle : V8DisposableSafeHandle
    {
        // Called by P/Invoke when returning SafeHandles
        private ContextHandle()
            : base(IntPtr.Zero, true)
        {
        }
    }

    internal sealed class ScriptHandle : V8DisposableSafeHandle
    {
        // Called by P/Invoke when returning SafeHandles
        private ScriptHandle()
            : base(IntPtr.Zero, true)
        {
        }
    }
}
