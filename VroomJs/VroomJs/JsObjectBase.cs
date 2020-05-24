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
    public abstract class JsObjectBase : DynamicObject, IDisposable
    {
        private readonly JsContext _context;
        private readonly IntPtr _objectHandle;
        private bool _disposed;

        internal JsObjectBase(JsContext context, IntPtr objectHandle)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (objectHandle == IntPtr.Zero)
                throw new ArgumentException("Invalid object handle", nameof(objectHandle));

            _context = context;
            _objectHandle = objectHandle;
        }

        public IEnumerable<string> GetMemberNames()
        {
            CheckDisposed();

            var v = NativeApi.jscontext_get_property_names(_context.Handle, _objectHandle);
            var res = Convert.FromJsValue(v);
            NativeApi.jsvalue_dispose(v);

            Exception e = res as JsException;
            if (e != null)
                throw e;

            var arr = (object[])res;
            return arr.Cast<string>();
        }


        public object GetPropertyValue(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            CheckDisposed();

            var v = NativeApi.jscontext_get_property_value(_context.Handle, _objectHandle, name);
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
            var v = NativeApi.jscontext_set_property_value(_context.Handle, _objectHandle, name, a);
            var res = Convert.FromJsValue(v);
            NativeApi.jsvalue_dispose(v);
            NativeApi.jsvalue_dispose(a);

            Exception e = res as JsException;
            if (e != null)
                throw e;
        }

        public object InvokeProperty(string name, object[] args)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            CheckDisposed();

            var a = JsValue.Null; // Null value unless we're given args.
            if (args != null)
                a = Convert.ToJsValue(args);

            var v = NativeApi.jscontext_invoke_property(_context.Handle, _objectHandle, name, a);
            var res = Convert.FromJsValue(v);
            NativeApi.jsvalue_dispose(v);
            NativeApi.jsvalue_dispose(a);

            Exception e = res as JsException;
            if (e != null)
                throw e;
            return res;
        }

        #region DynamicObject overrides

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = InvokeProperty(binder.Name, args);
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
            return GetMemberNames();
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

        ~JsObjectBase()
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


