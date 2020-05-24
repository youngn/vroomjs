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

namespace VroomJs
{
    public class JsObject : DynamicObject, IDisposable
    {
        private readonly JsContext _context;
        private readonly IntPtr _handle;
        private bool _disposed;

        internal JsObject(JsContext context, IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                throw new ArgumentException("Invalid pointer", nameof(ptr));

            _context = context;
            _handle = ptr;
        }

        internal IntPtr Handle
        {
            get { return _handle; }
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            CheckDisposed();

            result = _context.InvokeProperty(this, binder.Name, args);
            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            CheckDisposed();

            result = _context.GetPropertyValue(this, binder.Name);
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            CheckDisposed();

            _context.SetPropertyValue(this, binder.Name, value);
            return true;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            CheckDisposed();

            return _context.GetMemberNames(this);
        }

        #region IDisposable implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            _disposed = true;

            _context.Engine.DisposeObject(this.Handle);
        }

        ~JsObject()
        {
            if (!_disposed)
                Dispose(false);
        }

        #endregion
        
        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(JsObject));
        }
    }
}


