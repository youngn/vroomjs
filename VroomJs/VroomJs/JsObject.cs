// This file is part of the VroomJs library.
//
// Author:
//     Federico Di Gregorio <fog@initd.org>
//
// Copyright (c) 2013 
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
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace VroomJs
{
    public class JsObject : DynamicObject, IDisposable
    {
        private readonly JsContext _context;
        private readonly IntPtr _objectHandle;
        private bool _disposed;

        internal JsObject(JsContext context, IntPtr objectHandle)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (objectHandle == IntPtr.Zero)
                throw new ArgumentException("Invalid object handle", nameof(objectHandle));

            _context = context;
            _objectHandle = objectHandle;
        }

        public IEnumerable<object> GetPropertyNames()
        {
            CheckDisposed();

            // todo: make this more efficient by marshaling as a string array, rather than a JsArray
            var v = NativeApi.jsobject_get_property_names(_context.Handle, _objectHandle);
            var res = Convert.FromJsValue(v);
            NativeApi.jsvalue_dispose(v);

            Exception e = res as JsException;
            if (e != null)
                throw e;

            var arr = (JsArray)res;
            return arr.Cast<object>();
        }

        public object GetPropertyValue(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            CheckDisposed();

            var v = NativeApi.jsobject_get_named_property_value(_context.Handle, _objectHandle, name);
            var res = Convert.FromJsValue(v);
            NativeApi.jsvalue_dispose(v);

            Exception e = res as JsException;
            if (e != null)
                throw e;
            return res;
        }

        public object GetPropertyValue(int index)
        {
            CheckDisposed();

            var v = NativeApi.jsobject_get_indexed_property_value(_context.Handle, _objectHandle, index);
            var res = Convert.FromJsValue(v);
            NativeApi.jsvalue_dispose(v);

            Exception e = res as JsException;
            if (e != null)
                throw e;
            return res;
        }

        public void SetPropertyValue(string name, object value)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            CheckDisposed();

            var a = Convert.ToJsValue(value);
            var v = NativeApi.jsobject_set_named_property_value(_context.Handle, _objectHandle, name, a);
            var res = Convert.FromJsValue(v);
            NativeApi.jsvalue_dispose(v);
            NativeApi.jsvalue_dispose(a);

            Exception e = res as JsException;
            if (e != null)
                throw e;
        }

        public void SetPropertyValue(int index, object value)
        {
            CheckDisposed();

            var a = Convert.ToJsValue(value);
            var v = NativeApi.jsobject_set_indexed_property_value(_context.Handle, _objectHandle, index, a);
            var res = Convert.FromJsValue(v);
            NativeApi.jsvalue_dispose(v);
            NativeApi.jsvalue_dispose(a);

            Exception e = res as JsException;
            if (e != null)
                throw e;
        }

        public object InvokeMethod(string name, params object[] args)
        {
            var func = GetPropertyValue(name) as JsFunction;
            if (func == null)
                throw new InvalidOperationException($"'{name}' is not a function.");

            return func.Invoke(this, args);
        }

        public object InvokeMethod(int index, params object[] args)
        {
            var func = GetPropertyValue(index) as JsFunction;
            if (func == null)
                throw new InvalidOperationException($"{index} is not a function.");

            return func.Invoke(this, args);
        }

        public object this[string name]
        {
            get { return GetPropertyValue(name); }
            set { SetPropertyValue(name, value); }
        }

        public object this[int index]
        {
            get { return GetPropertyValue(index); }
            set { SetPropertyValue(index, value); }
        }

        #region DynamicObject overrides

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = InvokeMethod(binder.Name, args);
            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = GetPropertyValue(binder.Name);
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            SetPropertyValue(binder.Name, value);
            return true;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return GetPropertyNames().Select(m => m.ToString());
        }

        #endregion

        #region IDisposable implementation

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
            _context.Engine.DisposeObject(_objectHandle);
        }

        ~JsObject()
        {
            Dispose(false);
        }

        #endregion

        internal IntPtr Handle
        {
            get { return _objectHandle; }
        }

        internal JsConvert Convert => _context.Convert;

        internal JsContext Context => _context;

        protected void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            _context.CheckDisposed();
        }
    }
}


