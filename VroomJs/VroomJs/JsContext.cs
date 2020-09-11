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
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Timers;
using VroomJs.Interop;

namespace VroomJs
{
    public class JsContext : V8Object<ContextHandle>
    {
        private readonly int _id;
        private readonly JsEngine _engine;

        // Keep objects passed to V8 alive even if no other references exist.
        private readonly IKeepAliveStore _keepalives;

        private ExceptionDispatchInfo _pendingExceptionInfo;
        private bool _timeoutExceeded;// todo: needs thread sync (use Interlocked int?)

        internal JsContext(int id, JsEngine engine)
            :base(InitHandle(id, engine), owner: engine)
        {
            _id = id;
            _engine = engine;

            _keepalives = new KeepAliveDictionaryStore();
        }

        private static ContextHandle InitHandle(int id, JsEngine engine)
        {
            return NativeApi.jscontext_new(id, engine.Handle);
        }

        internal int Id => _id;

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

        public JsScript Compile(string code, string resourceName = null)
        {
            if (code == null)
                throw new ArgumentNullException(nameof(code));

            CheckDisposed();

            return new JsScript(this, code, resourceName);
        }

        public object Execute(string code, string resourceName = null, TimeSpan? executionTimeout = null)
        {
            if (code == null)
                throw new ArgumentNullException(nameof(code));

            CheckDisposed();

            return ExecuteWithTimeout(() =>
            {
                return NativeApi.jscontext_execute(Handle, code, resourceName ?? "<Unnamed Script>");
            }, executionTimeout);
        }

        // todo: Is this really a good idea? Let's keep it private for now
        internal object GetGlobal()
        {
            CheckDisposed();
            var v = (JsValue)NativeApi.jscontext_get_global(Handle);
            return ExtractAndCheckReturnValue(v);
        }

        public object GetVariable(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            CheckDisposed();

            var v = (JsValue)NativeApi.jscontext_get_variable(Handle, name);
            return ExtractAndCheckReturnValue(v);
        }

        public void SetVariable(string name, object value)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            CheckDisposed();

            var v = (JsValue)NativeApi.jscontext_set_variable(Handle, name, JsValue.ForValue(value, this));

            // Extract the return value so that it gets cleaned up,
            // and we check the result of the operation for errors.
            ExtractAndCheckReturnValue(v);
        }

        // todo: is this needed? get rid of it?
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
            var v = (JsValue)NativeApi.jscontext_new_object(Handle);
            return (JsObject)ExtractAndCheckReturnValue(v);
        }

        public JsArray CreateArray(params object[] elements)
        {
            if (elements == null)
                throw new ArgumentNullException(nameof(elements));

            var v = (JsValue)NativeApi.jscontext_new_array(
                Handle,
                elements.Length,
                elements.Select(z => (jsvalue)JsValue.ForValue(z, this)).ToArray()
            );

            return (JsArray)ExtractAndCheckReturnValue(v);
        }

        // todo: is this needed? get rid of it?
        internal void Flush()
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

        protected override void DisposeCore()
        {
            // TODO: do we need to run through the collection and dispose each object?
            _keepalives.Clear();

            base.DisposeCore();
        }

        internal object ExecuteWithTimeout(Func<JsValue> func, TimeSpan? executionTimeout = null)
        {
            Timer timer = null;
            if (executionTimeout.HasValue)
            {
                timer = new Timer(executionTimeout.Value.TotalMilliseconds);
                timer.Elapsed += (sender, args) =>
                {
                    timer.Stop();
                    _timeoutExceeded = true;
                    _engine.TerminateExecution();
                };
                timer.Start();
            }

            try
            {
                return ExtractAndCheckReturnValue(func());
            }
            finally
            {
                timer?.Dispose();
            }
        }

        internal JsObject GetExceptionProxy(Exception ex)
        {
            return GetHostObjectProxy(ex, _engine.ExceptionTemplateId);
        }

        internal JsObject GetHostObjectProxy(object obj, int templateId)
        {
            var x = JsValue.ForHostObject(AddHostObject(obj), templateId);
            var v = (JsValue)NativeApi.jscontext_get_proxy(Handle, x);
            return (JsObject)ExtractAndCheckReturnValue(v);
        }

        internal object ExtractAndCheckReturnValue(JsValue value)
        {
            // TODO: Ideally we would move this block inside the test for Termination,
            // but for reasons that are not clear, the Termination is not always observed
            // (see HostErrorFilterTests for test cases that illustrate this) prior to
            // getting here... so for now we have to check for a pending exception even
            // when termination is has not been observed.
            if(_pendingExceptionInfo != null)
            {
                var exInfo = _pendingExceptionInfo; _pendingExceptionInfo = null;
                exInfo.Throw();
            }

            if (value.ValueType == JsValueType.Termination)
            {
                // There should only be 2 reasons for termation: timeout or pending exception.
                var timedOut = _timeoutExceeded; _timeoutExceeded = false;

                if (timedOut)
                    throw new JsExecutionTimedOutException();
            }

            var obj = value.Extract(this);
            if (value.ValueType == JsValueType.JsError)
            {
                var errorInfo = (JsErrorInfo)obj;
                var ex = (errorInfo.Name == "SyntaxError")
                    ? new JsSyntaxException(errorInfo)
                    : new JsException(errorInfo, errorInfo.ClrException);
                throw ex;
            }
            return obj;
        }

        internal void SetPendingException(Exception exception)
        {
            _pendingExceptionInfo = ExceptionDispatchInfo.Capture(exception);
        }
    }
}
