using System;
using System.Runtime.InteropServices;
using VroomJs.Interop;

namespace VroomJs
{
    public sealed class JsScript : IDisposable
    {
        private readonly int _id;
        private readonly HandleRef _scriptHandle;
        private readonly Action<int> _notifyDispose;
        private bool _disposed;

        internal JsScript(int id, JsContext context, string code, string resourceName, Action<int> notifyDispose)
        {
            _id = id;
            Context = context;
            _notifyDispose = notifyDispose;

            _scriptHandle = new HandleRef(this, NativeApi.jsscript_new(Context.Handle));

            var v = NativeApi.jsscript_compile(_scriptHandle, code, resourceName);
            Context.ExtractAndCheckReturnValue(v);
        }

        public object Execute(TimeSpan? executionTimeout = null)
        {
            CheckDisposed();

            return Context.ExecuteWithTimeout(() =>
            {
                return NativeApi.jsscript_execute(_scriptHandle);
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

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            NativeApi.jsscript_dispose(_scriptHandle);

            if(disposing)
                _notifyDispose(_id);
        }

        ~JsScript()
        {
            Dispose(false);
        }

        #endregion

        internal HandleRef Handle
        {
            get { return _scriptHandle; }
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
