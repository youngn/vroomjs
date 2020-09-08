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
            _notifyDispose = notifyDispose;

            _scriptHandle = new HandleRef(this, NativeApi.jsscript_new(context.Handle));

            var v = NativeApi.jsscript_compile(_scriptHandle, code, resourceName);
            context.ExtractAndCheckReturnValue(v);
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
    }
}
