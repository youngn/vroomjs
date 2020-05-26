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

        [FieldOffset(12)] private int Length; // Length of array or string or managed object keepalive index.
        [FieldOffset(12)] private int Index;

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
        public static JsValue ForJsArray(JsArray value)
        {
            Debug.Assert(value != null);
            return new JsValue { Type = JsValueType.JsArray, Ptr = value.Handle };
        }
        public static JsValue ForJsFunction(JsFunction value)
        {
            Debug.Assert(value != null);
            return new JsValue { Type = JsValueType.Function, Ptr = value.Handle };
        }
        public static JsValue ForJsObject(JsObject value)
        {
            Debug.Assert(value != null);
            return new JsValue { Type = JsValueType.JsObject, Ptr = value.Handle };
        }
        public static JsValue ForManagedError(int id)
        {
            return new JsValue { Type = JsValueType.ManagedError, Index = id };
        }
        public static JsValue ForManagedObject(int id)
        {
            return new JsValue { Type = JsValueType.Managed, Index = id };
        }

        #endregion

        #region Value extractors

        public bool BooleanValue()
        {
            Debug.Assert(Type == JsValueType.Boolean);
            return I32 != 0;
        }
        public int Int32Value()
        {
            Debug.Assert(Type == JsValueType.Integer);
            return I32;
        }
        public uint UInt32Value()
        {
            Debug.Assert(Type == JsValueType.Index);
            return (uint)I64;
        }
        public double NumberValue()
        {
            Debug.Assert(Type == JsValueType.Number);
            return Num;
        }
        public string StringValue()
        {
            Debug.Assert(Type == JsValueType.String || Type == JsValueType.StringError);
            return Marshal.PtrToStringUni(Ptr);
        }
        public double DateValue()
        {
            Debug.Assert(Type == JsValueType.Date);
            return Num;
        }
        public IntPtr JsArrayValue()
        {
            Debug.Assert(Type == JsValueType.JsArray);
            return Ptr;
        }
        public IntPtr JsFunctionValue()
        {
            Debug.Assert(Type == JsValueType.Function);
            return Ptr;
        }
        public IntPtr JsObjectValue()
        {
            Debug.Assert(Type == JsValueType.JsObject);
            return Ptr;
        }
        public int ManagedValue()
        {
            Debug.Assert(Type == JsValueType.Managed);
            return Length;
        }
        public JsError ErrorValue()
        {
            Debug.Assert(Type == JsValueType.Error);
            return (JsError)Marshal.PtrToStructure(Ptr, typeof(JsError));
        }

        #endregion

        public override string ToString()
        {
            return string.Format("[JsValue({0})]", Type);
        }
    }
}
