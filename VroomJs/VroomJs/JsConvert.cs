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
using System.Runtime.InteropServices;

namespace VroomJs
{
    internal class JsConvert
    {
        public JsConvert(JsContext context)
        {
            _context = context;
        }

        readonly JsContext _context;

        public object FromJsValue(JsValue v)
        {
#if DEBUG_TRACE_API
			Console.WriteLine("Converting Js value to .net");
#endif
			switch (v.Type) {
				case JsValueType.Empty:
				case JsValueType.Null:
					return null;

				case JsValueType.Boolean:
					return v.I32 != 0;

				case JsValueType.Integer:
					return v.I32;

				case JsValueType.Index:
					return (UInt32)v.I64;

				case JsValueType.Number:
					return v.Num;

				case JsValueType.String:
					return Marshal.PtrToStringUni(v.Ptr);

				case JsValueType.Date:
                    return DateTimeOffset.FromUnixTimeMilliseconds((long)v.Num);

				case JsValueType.Array: {
					var r = new object[v.Length];
					for (int i = 0; i < v.Length; i++) {
						var vi = (JsValue)Marshal.PtrToStructure(new IntPtr(v.Ptr.ToInt64() + (16 * i)), typeof(JsValue));
						r[i] = FromJsValue(vi);
					}
					return r;
				}

				case JsValueType.UnknownError:
					if (v.Ptr != IntPtr.Zero)
						return new JsException(Marshal.PtrToStringUni(v.Ptr));
					return new JsInteropException("unknown error without reason");

				case JsValueType.StringError:
					return new JsException(Marshal.PtrToStringUni(v.Ptr));

				case JsValueType.Managed:
					return _context.KeepAliveGet(v.Index);

				case JsValueType.ManagedError:
					Exception inner = _context.KeepAliveGet(v.Index) as Exception;
					string msg = null;
					if (v.Ptr != IntPtr.Zero) {
						msg = Marshal.PtrToStringUni(v.Ptr);
					} else {
						if (inner != null) {
							msg = inner.Message;
						}
					}
					return new JsException(msg, inner);
#if NET40
                case JsValueType.Wrapped:
                    return new JsObject(_context, v.Ptr);
#else
				case JsValueType.Dictionary:
            		return JsDictionaryObject(v);
#endif
				case JsValueType.Error:
            		return JsException.Create(this, (JsError)Marshal.PtrToStructure(v.Ptr, typeof(JsError)));

				case JsValueType.Function:
					var fa = new JsValue[2];
					for (int i = 0; i < 2; i++) {
						fa[i] = (JsValue)Marshal.PtrToStructure(new IntPtr(v.Ptr.ToInt64() + (16 * i)), typeof(JsValue));
					}
            		return new JsFunction(_context, fa[0].Ptr, fa[1].Ptr);
            	default:
                    throw new InvalidOperationException("unknown type code: " + v.Type);
            }           
        }

#if !NET40 
    	private JsObject JsDictionaryObject(JsValue v) 
		{
			JsObject obj = new JsObject();
			for (int i = 0; i < (v.Length * 2); i += 2) 
			{
				var key = (JsValue)Marshal.PtrToStructure(new IntPtr(v.Ptr.ToInt64() + (16 * i)), typeof(JsValue));
				var value = (JsValue)Marshal.PtrToStructure(new IntPtr(v.Ptr.ToInt64() + (16 * (i + 1))), typeof(JsValue));
				obj[(string)FromJsValue(key)] = FromJsValue(value);
			}
			return obj;
    	}
#endif

    	public JsValue ToJsValue(object obj)
        {
            if (obj == null)
                return new JsValue { Type = JsValueType.Null };

            Type type = obj.GetType();

            // Check for nullable types (we will cast the value out of the box later).

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                type = type.GetGenericArguments()[0];

            if (type == typeof(Boolean))
                return new JsValue { Type = JsValueType.Boolean, I32 = (bool)obj ? 1 : 0 };

            if (type == typeof(String) || type == typeof(Char)) {
                // We need to allocate some memory on the other side; will be free'd by unmanaged code.
                return NativeApi.jsvalue_alloc_string(obj.ToString());
            }

            if (type == typeof(Byte))
                return new JsValue { Type = JsValueType.Integer, I32 = (int)(Byte)obj };
            if (type == typeof(Int16))
                return new JsValue { Type = JsValueType.Integer, I32 = (int)(Int16)obj };
            if (type == typeof(UInt16))
                return new JsValue { Type = JsValueType.Integer, I32 = (int)(UInt16)obj };
            if (type == typeof(Int32))
                return new JsValue { Type = JsValueType.Integer, I32 = (int)obj };
            if (type == typeof(UInt32))
                return new JsValue { Type = JsValueType.Integer, I32 = (int)(UInt32)obj };

            if (type == typeof(Int64))
                return new JsValue { Type = JsValueType.Number, Num = (double)(Int64)obj };
            if (type == typeof(UInt64))
                return new JsValue { Type = JsValueType.Number, Num = (double)(UInt64)obj };
            if (type == typeof(Single))
                return new JsValue { Type = JsValueType.Number, Num = (double)(Single)obj };
            if (type == typeof(Double))
                return new JsValue { Type = JsValueType.Number, Num = (double)obj };
            if (type == typeof(Decimal))
                return new JsValue { Type = JsValueType.Number, Num = (double)(Decimal)obj };

            if (type == typeof(DateTimeOffset))
                return new JsValue {
                    Type = JsValueType.Date,
                    Num = ((DateTimeOffset)obj).ToUnixTimeMilliseconds()
                };

            if (type == typeof(JsObject))
                return new JsValue { Type = JsValueType.Wrapped, Ptr = ((JsObject)obj).Handle };

            // Arrays of anything that can be cast to object[] are recursively convertef after
            // allocating an appropriate jsvalue on the unmanaged side.

            var array = obj as object[];
            if (array != null) {
                JsValue v = NativeApi.jsvalue_alloc_array(array.Length);
                if (v.Length != array.Length)
                    throw new JsInteropException("can't allocate memory on the unmanaged side");
                for (int i=0 ; i < array.Length ; i++)
                    Marshal.StructureToPtr(ToJsValue(array[i]), new IntPtr(v.Ptr.ToInt64() + (16*i)), false);
                return v;
            }

            // Every object explicitly converted to a value becomes an entry of the
            // _keepalives list, to make sure the GC won't collect it while still in
            // use by the unmanaged Javascript engine. We don't try to track duplicates
            // because adding the same object more than one time acts more or less as
            // reference counting.

            return new JsValue { Type = JsValueType.Managed, Index = _context.KeepAliveAdd(obj) };
        }
    }
}
