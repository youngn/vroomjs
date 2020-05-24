using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace VroomJs
{
	public partial class JsContext
    {
		public IEnumerable<string> GetMemberNames(JsObject obj) 
		{
			if (obj == null)
				throw new ArgumentNullException("obj");

			CheckDisposed();

			if (obj.Handle == IntPtr.Zero)
				throw new JsInteropException("wrapped V8 object is empty (IntPtr is Zero)");

			JsValue v = NativeApi.jscontext_get_property_names(_context, obj.Handle);
			object res = _convert.FromJsValue(v);
            NativeApi.jsvalue_dispose(v);

			Exception e = res as JsException;
			if (e != null)
				throw e;

			object[] arr = (object[])res;
			return arr.Cast<string>();
		}


        public object GetPropertyValue(JsObject obj, string name)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            if (name == null)
                throw new ArgumentNullException("name");

            CheckDisposed();

            if (obj.Handle == IntPtr.Zero)
                throw new JsInteropException("wrapped V8 object is empty (IntPtr is Zero)");

			JsValue v = NativeApi.jscontext_get_property_value(_context, obj.Handle, name);
            object res = _convert.FromJsValue(v);
            NativeApi.jsvalue_dispose(v);

            Exception e = res as JsException;
            if (e != null)
                throw e;
            return res;
        }

        public void SetPropertyValue(JsObject obj, string name, object value)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            if (name == null)
                throw new ArgumentNullException("name");

            CheckDisposed();

            if (obj.Handle == IntPtr.Zero)
                throw new JsInteropException("wrapped V8 object is empty (IntPtr is Zero)");

            JsValue a = _convert.ToJsValue(value);
			JsValue v = NativeApi.jscontext_set_property_value(_context, obj.Handle, name, a);
            object res = _convert.FromJsValue(v);
            NativeApi.jsvalue_dispose(v);
            NativeApi.jsvalue_dispose(a);

            Exception e = res as JsException;
            if (e != null)
                throw e;
        }

        public object InvokeProperty(JsObject obj, string name, object[] args)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            if (name == null)
                throw new ArgumentNullException("name");

            CheckDisposed();

            if (obj.Handle == IntPtr.Zero)
                throw new JsInteropException("wrapped V8 object is empty (IntPtr is Zero)");

            JsValue a = JsValue.Null; // Null value unless we're given args.
            if (args != null)
                a = _convert.ToJsValue(args);

			JsValue v = NativeApi.jscontext_invoke_property(_context, obj.Handle, name, a);
            object res = _convert.FromJsValue(v);
            NativeApi.jsvalue_dispose(v);
            NativeApi.jsvalue_dispose(a);

            Exception e = res as JsException;
            if (e != null)
                throw e;
            return res;
        }
	}
}
