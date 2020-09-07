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
using System.Runtime.Serialization;

namespace VroomJs
{

    [Serializable]
    public class JsException : Exception
    {
        public JsException(string message)
            : base(message)
        {
        }

        public JsException(string message, Exception inner)
            : base(message, inner)
        {

        }

        protected JsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        internal JsException(JsErrorInfo errorInfo, Exception inner)
            : base(FormatMessage(errorInfo.Text, errorInfo.StackTrace), inner)
        {
            ErrorInfo = errorInfo;
        }

        public JsErrorInfo ErrorInfo { get; }

        private static string FormatMessage(string text, JsStackTrace stackTrace)
        {
            return $"{text}\n{stackTrace}";
        }
    }

    // todo: should this really be derived from JsException? i.e. can it have ErrorInfo?
    public class JsSyntaxException : JsException
    {
        internal JsSyntaxException(JsErrorInfo errorInfo)
            : base(errorInfo, inner: null)
        {
        }
    }
}
