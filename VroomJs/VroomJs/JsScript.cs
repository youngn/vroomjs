using System;
using System.Runtime.InteropServices;
using VroomJs.Interop;

namespace VroomJs
{
    public sealed class JsScript : IDisposable
    {
        private readonly int _id;
        private readonly ScriptHandle _handle;
        private readonly Action<int> _notifyDispose;
        private bool _disposed;

        internal JsScript(int id, JsContext context, string code, string resourceName, Action<int> notifyDispose)
        {
            _id = id;
            Context = context;
            _notifyDispose = notifyDispose;

            _handle = NativeApi.jsscript_new(Context.Handle);

            var v = NativeApi.jsscript_compile(_handle, code, resourceName);
            Context.ExtractAndCheckReturnValue(v);
        }

        public object Execute(TimeSpan? executionTimeout = null)
        {
            CheckDisposed();

            return Context.ExecuteWithTimeout(() =>
            {
                return NativeApi.jsscript_execute(_handle);
            }, executionTimeout);
        }

        #region IDisposable implementation

        public bool IsDisposed
        {
            get { return _disposed; }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            _handle.Dispose();
            _notifyDispose(_id);
        }

        #endregion

        internal ScriptHandle Handle
        {
            get { return _handle; }
        }

        internal JsContext Context { get; }

        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(JsScript));

            Context.CheckDisposed();
        }
    }
}
