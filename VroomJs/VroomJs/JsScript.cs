using System;
using System.Runtime.InteropServices;
using VroomJs.Interop;

namespace VroomJs
{
    // todo: sealed?
    public class JsScript : IDisposable
    {
        private readonly int _id;
        private readonly JsEngine _engine;
        private readonly HandleRef _scriptHandle;
        private readonly Action<int> _notifyDispose;
        private bool _disposed;

        internal JsScript(int id, JsEngine engine, HandleRef engineHandle, JsContext context, string code, string name, Action<int> notifyDispose)
        {
            _id = id;
            _engine = engine;
            _notifyDispose = notifyDispose;

            _scriptHandle = new HandleRef(this, NativeApi.jsscript_new(engineHandle));

            JsValue v = NativeApi.jsscript_compile(_scriptHandle, code, name);
            object res = v.Extract(context);
            Exception e = res as JsException;
            if (e != null)
            {
                throw e;
            }
        }

        public JsEngine Engine
        {
            get { return _engine; }
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

        protected virtual void Dispose(bool disposing)
        {
            NativeApi.jsscript_dispose(_scriptHandle);

            if(disposing)
            {
                _notifyDispose(_id);
            }
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
        private void CheckDisposed()
        {
            if (_engine.IsDisposed)
            {
                throw new ObjectDisposedException("JsScript: engine has been disposed");
            }
            if (_disposed)
                throw new ObjectDisposedException("JsScript:" + _scriptHandle.Handle);
        }
    }
}
