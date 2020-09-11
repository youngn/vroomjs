using System;
using System.Runtime.InteropServices;
using VroomJs.Interop;

namespace VroomJs
{
    public abstract class V8DisposableSafeHandle : SafeHandle
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

    public sealed class EngineHandle : V8DisposableSafeHandle
    {
        // Called by P/Invoke when returning SafeHandles
        private EngineHandle()
            : base(IntPtr.Zero, true)
        {
        }
    }

    public sealed class ContextHandle : V8DisposableSafeHandle
    {
        // Called by P/Invoke when returning SafeHandles
        private ContextHandle()
            : base(IntPtr.Zero, true)
        {
        }
    }

    public sealed class ScriptHandle : V8DisposableSafeHandle
    {
        // Called by P/Invoke when returning SafeHandles
        private ScriptHandle()
            : base(IntPtr.Zero, true)
        {
        }
    }

    public sealed class ObjectHandle : V8DisposableSafeHandle
    {
        // Called by P/Invoke when returning SafeHandles
        private ObjectHandle()
            : base(IntPtr.Zero, true)
        {
        }

        // If & only if you need to support user-supplied handles
        internal ObjectHandle(IntPtr preexistingHandle, bool ownsHandle)
            : base(IntPtr.Zero, ownsHandle)
        {
            SetHandle(preexistingHandle);
        }
    }
}
