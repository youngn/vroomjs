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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace VroomJs
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct JsValue
    {
        [FieldOffset(0)] private int I32;
        [FieldOffset(0)] private long I64;
        [FieldOffset(0)] private double Num;
        [FieldOffset(0)] private IntPtr Ptr;

        [FieldOffset(8)] public JsValueType Type; // marshaled as integer.

        [FieldOffset(12)] private int Length; // Length of array or string

        public static JsValue ForValue(object obj, JsContext context)
        {
            if (obj == null)
                return ForNull();

            var type = obj.GetType();

            // Check for nullable types (we will cast the value out of the box later).

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                type = type.GetGenericArguments()[0];

            if (type == typeof(bool))
                return ForBoolean((bool)obj);

            if (type == typeof(string) || type == typeof(char))
            {
                // We need to allocate some memory on the other side; will be free'd by unmanaged code.
                //return NativeApi.jsvalue_alloc_string(obj.ToString());
                return ForJsString(obj.ToString(), context);
            }

            if (type == typeof(byte))
                return ForInt32((byte)obj);
            if (type == typeof(short))
                return ForInt32((short)obj);
            if (type == typeof(ushort))
                return ForInt32((ushort)obj);
            if (type == typeof(int))
                return ForInt32((int)obj);
            if (type == typeof(uint))
                return ForInt32((int)(uint)obj);

            if (type == typeof(long))
                return ForNumber((long)obj); // todo: overflow?
            if (type == typeof(ulong))
                return ForNumber((ulong)obj); // todo: overflow?
            if (type == typeof(float))
                return ForNumber((float)obj);
            if (type == typeof(double))
                return ForNumber((double)obj);
            if (type == typeof(decimal))
                return ForNumber((double)(decimal)obj);

            if (type == typeof(DateTimeOffset))
                return ForDate(((DateTimeOffset)obj).ToUnixTimeMilliseconds());

            if (type == typeof(JsObject))
                return ForJsObject((JsObject)obj);

            if (type == typeof(JsArray))
                return ForJsArray((JsArray)obj);

            if (type == typeof(JsFunction))
                return ForJsFunction((JsFunction)obj);

            // Every object explicitly converted to a value becomes an entry of the
            // _keepalives list, to make sure the GC won't collect it while still in
            // use by the unmanaged Javascript engine.
            return ForClrObject(context.KeepAliveAdd(obj));
        }

        public object Extract(JsContext context)
        {
            var result = GetValue(context);
            Dispose();
            return result;
        }

        public override string ToString()
        {
            return string.Format("[JsValue({0})]", Type);
        }

        #region Factory methods

        public static JsValue ForEmpty()
        {
            return new JsValue() { Type = JsValueType.Empty };
        }
        public static JsValue ForUnknownError()
        {
            return new JsValue { Type = JsValueType.UnknownError };
        }
        public static JsValue ForNull()
        {
            return new JsValue { Type = JsValueType.Null };
        }
        public static JsValue ForBoolean(bool value)
        {
            return new JsValue { Type = JsValueType.Boolean, I32 = value ? 1 : 0 };
        }
        public static JsValue ForInt32(int value)
        {
            return new JsValue { Type = JsValueType.Integer, I32 = value };
        }
        public static JsValue ForUInt32(uint value)
        {
            return new JsValue { Type = JsValueType.Index, I64 = value };
        }
        public static JsValue ForNumber(double value)
        {
            return new JsValue { Type = JsValueType.Number, Num = value };
        }
        public static JsValue ForDate(double value)
        {
            return new JsValue { Type = JsValueType.Date, Num = value };
        }
        private static JsValue ForJsString(string value, JsContext context)
        {
            var v = NativeApi.jsstring_new(context.Engine.Handle, value);
            if (v.Type == JsValueType.Empty)
                throw new JsInteropException("String exceeds maximum allowable length."); //todo:test?
            return v;
        }
        public static JsValue ForJsArray(JsArray value)
        {
            Debug.Assert(value != null);
            return new JsValue { Type = JsValueType.JsArray, Ptr = value.Handle };
        }
        public static JsValue ForJsFunction(JsFunction value)
        {
            Debug.Assert(value != null);
            return new JsValue { Type = JsValueType.JsFunction, Ptr = value.Handle };
        }
        public static JsValue ForJsObject(JsObject value)
        {
            Debug.Assert(value != null);
            return new JsValue { Type = JsValueType.JsObject, Ptr = value.Handle };
        }
        public static JsValue ForClrError(int id)
        {
            return new JsValue { Type = JsValueType.ClrError, I32 = id };
        }
        public static JsValue ForClrObject(int id)
        {
            return new JsValue { Type = JsValueType.ClrObject, I32 = id };
        }

        #endregion

        #region Value getters

        private object GetValue(JsContext context)
        {
#if DEBUG_TRACE_API
			Console.WriteLine("Converting Js value to .net");
#endif
            switch (Type)
            {
                case JsValueType.Empty:
                case JsValueType.Null:
                    return null;

                case JsValueType.Boolean:
                    return BooleanValue();

                case JsValueType.Integer:
                    return Int32Value();

                case JsValueType.Index:
                    return UInt32Value();

                case JsValueType.Number:
                    return NumberValue();

                case JsValueType.String:
                    return StringValue();

                case JsValueType.JsString:
                    return JsStringValue(context);

                case JsValueType.Date:
                    return DateValue();

                case JsValueType.UnknownError:
                    return new JsInteropException("unknown error without reason"); // todo: improve this

                case JsValueType.ClrObject:
                    return ClrObjectValue(context);

                case JsValueType.ClrError:
                    // todo: do we really want to wrapping in JsException? maybe better to just rethrow raw?
                    var inner = ClrObjectValue(context) as Exception;
                    var msg = inner?.Message ?? "Unknown error"; // todo: make this better
                    return new JsException(msg, inner);

                case JsValueType.JsObject:
                    return JsObjectValue(context);

                case JsValueType.JsArray:
                    return JsArrayValue(context);

                case JsValueType.JsFunction:
                    return JsFunctionValue(context);

                case JsValueType.JsError:
                    return JsErrorValue(context);

                default:
                    throw new InvalidOperationException("unknown type code: " + Type);
            }
        }

        private bool BooleanValue()
        {
            Debug.Assert(Type == JsValueType.Boolean);
            return I32 != 0;
        }
        private int Int32Value()
        {
            Debug.Assert(Type == JsValueType.Integer);
            return I32;
        }
        private uint UInt32Value()
        {
            Debug.Assert(Type == JsValueType.Index);
            return (uint)I64;
        }
        private double NumberValue()
        {
            Debug.Assert(Type == JsValueType.Number);
            return Num;
        }
        private string StringValue()
        {
            Debug.Assert(Type == JsValueType.String);
            return Marshal.PtrToStringUni(Ptr);
        }
        private object JsStringValue(JsContext context)
        {
            Debug.Assert(Type == JsValueType.JsString);

            // The value of the string is copied into the buffer with no null terminator.
            var buffer = new char[Length];
            var n = NativeApi.jsstring_get_value(context.Engine.Handle, Ptr, buffer);
            if (n != Length)
                throw new JsInteropException("Failed to copy string.");
            return new string(buffer);
        }
        private DateTimeOffset DateValue()
        {
            Debug.Assert(Type == JsValueType.Date);
            return DateTimeOffset.FromUnixTimeMilliseconds((long)Num);
        }
        private JsArray JsArrayValue(JsContext context)
        {
            Debug.Assert(Type == JsValueType.JsArray);
            return new JsArray(context, Ptr);
        }
        private JsFunction JsFunctionValue(JsContext context)
        {
            Debug.Assert(Type == JsValueType.JsFunction);
            return new JsFunction(context, Ptr);
        }
        private JsObject JsObjectValue(JsContext context)
        {
            Debug.Assert(Type == JsValueType.JsObject);
            return new JsObject(context, Ptr);
        }
        private object ClrObjectValue(JsContext context)
        {
            Debug.Assert(Type == JsValueType.ClrObject || Type == JsValueType.ClrError);
            return context.KeepAliveGet(I32);
        }

        private JsException JsErrorValue(JsContext context)
        {
            Debug.Assert(Type == JsValueType.JsError);

            var info = Marshal.PtrToStructure<JsErrorInfo>(Ptr);

            var resource = info.Resource != null ? Marshal.PtrToStringUni(info.Resource) : null;
            var description = info.Description != null ? Marshal.PtrToStringUni(info.Description) : null;
            var type = info.Type != null ? Marshal.PtrToStringUni(info.Type) : null;
            var text = info.Text != null ? Marshal.PtrToStringUni(info.Text) : null;
            var stackStr = info.StackStr != null ? Marshal.PtrToStringUni(info.StackStr) : null;

            var line = info.Line;
            var column = info.Column;

            if (type == "SyntaxError")
            {
                // todo: do we actually get a JS error object here? If so, include it
                return new JsSyntaxException(description, resource, line, column, type, text);
            }

            // The error object can be anything is JS - it is not necessarily an Error object,
            // or even an Object, so we don't cast it.
            var error = info.Error.GetValue(context);

            var stackTrace = GetStackFrames(info.StackFrames);

            return new JsException(description, resource, line, column, error, text, type, stackStr,
                new JsStackTrace(stackTrace.ToList()));
        }

        private IEnumerable<JsStackTrace.Frame> GetStackFrames(IntPtr stackFrame)
        {
            while(stackFrame != IntPtr.Zero)
            {
                var info = Marshal.PtrToStructure<JsStackFrame>(stackFrame);

                var resource = info.Resource != null ? Marshal.PtrToStringUni(info.Resource) : null;
                var function = info.Function != null ? Marshal.PtrToStringUni(info.Function) : null;
                var line = info.Line;
                var column = info.Column;

                yield return new JsStackTrace.Frame(resource, function, line, column);

                stackFrame = info.Next;
            }
        }

        #endregion

        private void Dispose()
        {
            // Dispose of any unmanaged resources.
            // For efficiency, we only do the pinvoke if we know that the type has
            // unmanaged resources that require disposal.
            switch (Type)
            {
                case JsValueType.String:
                case JsValueType.JsString:
                case JsValueType.JsError:
                    NativeApi.jsvalue_dispose(this);
                    break;
            }
        }
    }
}
