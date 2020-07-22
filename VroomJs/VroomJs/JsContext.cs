// This file is part of the VroomJs library.
//
// Author:
//     Federico Di Gregorio <fog@initd.org>
//
// Copyright © 2013 Federico Di Gregorio <fog@initd.org>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Timers;
using VroomJs.Interop;

namespace VroomJs
{
    public partial class JsContext : IDisposable
    {
        private readonly int _id;
        private readonly JsEngine _engine;
        private readonly HandleRef _contextHandle;

        // Keep objects passed to V8 alive even if no other references exist.
        private readonly IKeepAliveStore _keepalives;

        private readonly Action<int> _notifyDispose;
        private bool _disposed;

        internal JsContext(int id, JsEngine engine, HandleRef engineHandle, Action<int> notifyDispose)
        {
            _id = id;
            _engine = engine;
            _notifyDispose = notifyDispose;

            _keepalives = new KeepAliveDictionaryStore();
            _contextHandle = new HandleRef(this, NativeApi.jscontext_new(id, engineHandle));
        }

        public JsEngine Engine
        {
            get { return _engine; }
        }

        public JsEngineStats GetStats()
        {
            return new JsEngineStats
            {
                KeepAliveMaxSlots = _keepalives.MaxSlots,
                KeepAliveAllocatedSlots = _keepalives.AllocatedSlots,
                KeepAliveUsedSlots = _keepalives.UsedSlots
            };
        }

        // NEED TESTS
        public object Execute(JsScript script, TimeSpan? executionTimeout = null)
        {
            if (script == null)
                throw new ArgumentNullException("script");

            CheckDisposed();

            bool executionTimedOut = false;
            Timer timer = null;
            if (executionTimeout.HasValue)
            {
                timer = new Timer(executionTimeout.Value.TotalMilliseconds);
                timer.Elapsed += (sender, args) =>
                {
                    timer.Stop();
                    executionTimedOut = true;
                    _engine.TerminateExecution();
                };
                timer.Start();
            }
            object res;
            try
            {
                var v = (JsValue)NativeApi.jscontext_execute_script(_contextHandle, script.Handle);
                res = v.Extract(this);
            }
            finally
            {
                if (executionTimeout.HasValue)
                {
                    timer.Dispose();
                }
            }

            if (executionTimedOut)
            {
                throw new JsExecutionTimedOutException();
            }

            Exception e = res as JsException;
            if (e != null)
                throw e;
            return res;
        }

        public object Execute(string code, string name = null, TimeSpan? executionTimeout = null)
        {
            Stopwatch watch1 = new Stopwatch();
            Stopwatch watch2 = new Stopwatch();

            watch1.Start();
            if (code == null)
                throw new ArgumentNullException("code");

            CheckDisposed();

            bool executionTimedOut = false;
            Timer timer = null;
            if (executionTimeout.HasValue)
            {
                timer = new Timer(executionTimeout.Value.TotalMilliseconds);
                timer.Elapsed += (sender, args) =>
                {
                    timer.Stop();
                    executionTimedOut = true;
                    _engine.TerminateExecution();
                };
                timer.Start();
            }
            object res;
            try
            {
                watch2.Start();
                var v = (JsValue)NativeApi.jscontext_execute(_contextHandle, code, name ?? "<Unnamed Script>");
                watch2.Stop();
                res = v.Extract(this);
            }
            finally
            {
                if (executionTimeout.HasValue)
                {
                    timer.Dispose();
                }
            }

            if (executionTimedOut)
            {
                throw new JsExecutionTimedOutException();
            }

            Exception e = res as JsException;
            if (e != null)
                throw e;
            watch1.Stop();

            // Console.WriteLine("Execution time " + watch2.ElapsedTicks + " total time " + watch1.ElapsedTicks);
            return res;
        }

        // todo: Is this really a good idea? Let's keep it private for now
        internal object GetGlobal()
        {
            CheckDisposed();
            var v = (JsValue)NativeApi.jscontext_get_global(_contextHandle);
            object res = v.Extract(this);

            Exception e = res as JsException;
            if (e != null)
                throw e;
            return res;
        }

        public object GetVariable(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            CheckDisposed();

            var v = (JsValue)NativeApi.jscontext_get_variable(_contextHandle, name);
            object res = v.Extract(this);

            Exception e = res as JsException;
            if (e != null)
                throw e;
            return res;
        }

        public void SetVariable(string name, object value)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            CheckDisposed();

            var a = JsValue.ForValue(value, this);
            var b = (JsValue)NativeApi.jscontext_set_variable(_contextHandle, name, a);

            // Extract the return value so that it gets cleaned up, even if not used
            var result = b.Extract(this);
            // TODO: Check the result of the operation for errors.
        }

        // NEED TESTS
        public void SetFunction(string name, Delegate func)
        {
            WeakDelegate del;
            if (func.Target != null)
            {
                del = new BoundWeakDelegate(func.Target, func.Method.Name);
            }
            else
            {
                del = new BoundWeakDelegate(func.Method.DeclaringType, func.Method.Name);
            }
            SetVariable(name, del);
        }

        public JsObject CreateObject()
        {
            var v = (JsValue)NativeApi.jscontext_new_object(_contextHandle);
            return (JsObject)v.Extract(this);
        }

        public JsArray CreateArray(params object[] elements)
        {
            if (elements == null)
                throw new ArgumentNullException(nameof(elements));

            var v = (JsValue)NativeApi.jscontext_new_array(
                _contextHandle,
                elements.Length,
                elements.Select(z => (jsvalue)JsValue.ForValue(z, this)).ToArray()
            );

            return (JsArray)v.Extract(this);
        }

        public void Flush()
        {
            NativeApi.jscontext_force_gc();
        }

        #region Host object management

        internal int AddHostObject(object obj)
        {
            return _keepalives.Insert(obj);
        }

        internal object GetHostObject(int slot)
        {
            return _keepalives.Get(slot);
        }

        internal void RemoveHostObject(int slot)
        {
            _keepalives.Remove(slot);
        }

        #endregion

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
            NativeApi.jscontext_dispose(_contextHandle);

            if (disposing)
            {
                // TODO: do we need to run through the collection and dispose each object?
                _keepalives.Clear();
                _notifyDispose(_id);
            }
        }

        ~JsContext()
        {
            Dispose(false);
        }

        #endregion

        internal HandleRef Handle
        {
            get { return _contextHandle; }
        }

        internal void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(JsContext));

            _engine.CheckDisposed();
        }

        internal JsObject GetExceptionProxy(Exception ex)
        {
            return GetHostObjectProxy(ex, _engine.ExceptionTemplateId);
        }

        internal JsObject GetHostObjectProxy(object obj, int templateId)
        {
            var x = JsValue.ForHostObject(AddHostObject(obj), templateId);
            var v = (JsValue)NativeApi.jscontext_get_proxy(_contextHandle, x);
            return (JsObject)v.Extract(this);
        }
    }
}
