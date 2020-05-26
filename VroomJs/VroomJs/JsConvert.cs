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

namespace VroomJs
{
    internal class JsConvert
    {
        private readonly JsContext _context;

        public JsConvert(JsContext context)
        {
            _context = context;
        }

        public object FromJsValue(JsValue v)
        {
#if DEBUG_TRACE_API
			Console.WriteLine("Converting Js value to .net");
#endif
            switch (v.Type)
            {
                case JsValueType.Empty:
                case JsValueType.Null:
                    return null;

                case JsValueType.Boolean:
                    return v.BooleanValue();

                case JsValueType.Integer:
                    return v.Int32Value();

                case JsValueType.Index:
                    return v.UInt32Value();

                case JsValueType.Number:
                    return v.NumberValue();

                case JsValueType.String:
                    return v.StringValue();

                case JsValueType.Date:
                    return DateTimeOffset.FromUnixTimeMilliseconds((long)v.DateValue());

                //case JsValueType.Array:
                //    {
                //        var r = new object[v.Length];
                //        for (int i = 0; i < v.Length; i++)
                //        {
                //            var vi = (JsValue)Marshal.PtrToStructure(new IntPtr(v.Ptr.ToInt64() + (16 * i)), typeof(JsValue));
                //            r[i] = FromJsValue(vi);
                //        }
                //        return r;
                //    }

                case JsValueType.UnknownError:
                    return new JsInteropException("unknown error without reason"); // todo: improve this

                case JsValueType.StringError: // todo: is still used?
                    return new JsException(v.StringValue());

                case JsValueType.Managed:
                    return _context.KeepAliveGet(v.ManagedValue());

                case JsValueType.ManagedError:
                    // todo: do we really want to wrapping in JsException? maybe better to just rethrow raw?
                    var inner = _context.KeepAliveGet(v.ManagedValue()) as Exception;
                    var msg = inner?.Message ?? "Unknown error"; // todo: make this better
                    return new JsException(msg, inner);

                case JsValueType.JsObject:
                    return new JsObject(_context, v.JsObjectValue());

                case JsValueType.JsArray:
                    return new JsArray(_context, v.JsArrayValue());

                case JsValueType.Function:
                    return new JsFunction(_context, v.JsFunctionValue());

                case JsValueType.Error:
                    return JsException.Create(this, v.ErrorValue());

                default:
                    throw new InvalidOperationException("unknown type code: " + v.Type);
            }
        }

        public JsValue ToJsValue(object obj)
        {
            if (obj == null)
                return JsValue.ForNull();

            var type = obj.GetType();

            // Check for nullable types (we will cast the value out of the box later).

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                type = type.GetGenericArguments()[0];

            if (type == typeof(bool))
                return JsValue.ForBoolean((bool)obj);

            if (type == typeof(string) || type == typeof(char))
            {
                // We need to allocate some memory on the other side; will be free'd by unmanaged code.
                return NativeApi.jsvalue_alloc_string(obj.ToString());
            }

            if (type == typeof(Byte))
                return JsValue.ForInt32((int)(Byte)obj);
            if (type == typeof(Int16))
                return JsValue.ForInt32((int)(Int16)obj);
            if (type == typeof(UInt16))
                return JsValue.ForInt32((int)(UInt16)obj);
            if (type == typeof(Int32))
                return JsValue.ForInt32((int)obj);
            if (type == typeof(UInt32))
                return JsValue.ForInt32((int)(UInt32)obj);

            if (type == typeof(Int64))
                return JsValue.ForNumber((double)(Int64)obj); // todo: overflow?
            if (type == typeof(UInt64))
                return JsValue.ForNumber((double)(UInt64)obj); // todo: overflow?
            if (type == typeof(Single))
                return JsValue.ForNumber((double)(Single)obj);
            if (type == typeof(Double))
                return JsValue.ForNumber((double)obj);
            if (type == typeof(Decimal))
                return JsValue.ForNumber((double)(Decimal)obj);

            if (type == typeof(DateTimeOffset))
                return JsValue.ForDate(((DateTimeOffset)obj).ToUnixTimeMilliseconds());

            if (type == typeof(JsObject))
                return JsValue.ForJsObject((JsObject)obj);

            if (type == typeof(JsArray))
                return JsValue.ForJsArray((JsArray)obj);

            if (type == typeof(JsFunction))
                return JsValue.ForJsFunction((JsFunction)obj);

            // Arrays of anything that can be cast to object[] are recursively convertef after
            // allocating an appropriate jsvalue on the unmanaged side.
            //var array = obj as object[];
            //if (array != null)
            //{
            //    JsValue v = NativeApi.jsvalue_alloc_array(array.Length);
            //    if (v.Length != array.Length)
            //        throw new JsInteropException("can't allocate memory on the unmanaged side");
            //    for (int i = 0; i < array.Length; i++)
            //        Marshal.StructureToPtr(ToJsValue(array[i]), new IntPtr(v.Ptr.ToInt64() + (16 * i)), false);
            //    return v;
            //}

            // Every object explicitly converted to a value becomes an entry of the
            // _keepalives list, to make sure the GC won't collect it while still in
            // use by the unmanaged Javascript engine. We don't try to track duplicates
            // because adding the same object more than one time acts more or less as
            // reference counting.
            return JsValue.ForManagedObject(_context.KeepAliveAdd(obj));
        }
    }
}
