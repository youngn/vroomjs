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
        private readonly string _resource;
        private readonly int _line;
        private readonly int _column;
        private readonly string _type;
        private readonly string _text;
        private readonly object _error;
        private readonly string _description;
        private readonly string _stackStr;
        private readonly JsStackTrace _stackTrace;

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

        internal JsException(string description, string resource, int line, int col,
            object error, string text, string type, string stackStr,
            JsStackTrace stackTrace)
            : base(FormatMessage(text, stackTrace))
        {
            _description = description;
            _resource = resource;
            _line = line;
            _column = col;
            _error = error;
            _text = text;
            _type = type;
            _stackStr = stackStr;
            _stackTrace = stackTrace;
        }

        /// <summary>
        /// Gets the description of the error as provided by V8.
        /// </summary>
        public string Description => _description;

        /// <summary>
        /// Gets the resource containing the script from which the error was thrown.
        /// </summary>
        public string Resource => _resource;

        /// <summary>
        /// Gets the line number at which the error was thrown.
        /// </summary>
        public int Line => _line;

        /// <summary>
        /// Gets the column at which the error was thrown.
        /// </summary>
        public int Column => _column;

        /// <summary>
        /// Gets the error object (the object thrown by the 'throw' statement).
        /// </summary>
        public object Error => _error;

        /// <summary>
        /// Gets the result of the .toString() method of the error object.
        /// </summary>
        public string ErrorText => _text;

        /// <summary>
        /// Gets the value of the .name property of the error object; may be null.
        /// </summary>
        public string ErrorName => _type;

        /// <summary>
        /// Gets the value of the .stack property of the error object; may be null.
        /// </summary>
        public string ErrorStackString => _stackStr;

        /// <summary>
        /// Gets the stack trace.
        /// </summary>
        public JsStackTrace ErrorStackTrace => _stackTrace;

        private static string FormatMessage(string text, JsStackTrace stackTrace)
        {
            return $"{text}\n{stackTrace}";
        }
    }

    public class JsSyntaxException : JsException
    {
        internal JsSyntaxException(string description, string resource, int line, int col, string type, string text)
            : base(description, resource, line, col, null, text, type, null, null)
        {
        }
    }
}
