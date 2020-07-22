namespace VroomJs
{
    public class JsErrorInfo
    {
        internal JsErrorInfo(
            string resource,
            int line,
            int col,
            object error,
            string text,
            string type,
            string stackStr,
            JsStackTrace stackTrace)
        {
            Resource = resource;
            Line = line;
            Column = col;
            Error = error;
            Text = text;
            Name = type;
            StackString = stackStr;
            StackTrace = stackTrace;
        }

        /// <summary>
        /// Gets the resource containing the script from which the error was thrown.
        /// </summary>
        public string Resource { get; }

        /// <summary>
        /// Gets the line number at which the error was thrown.
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// Gets the column at which the error was thrown.
        /// </summary>
        public int Column { get; }

        /// <summary>
        /// Gets the error object (the object thrown by the 'throw' statement).
        /// </summary>
        public object Error { get; }

        /// <summary>
        /// Gets the result of the .toString() method of the error object.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Gets the value of the .name property of the error object; may be null.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the value of the .stack property of the error object; may be null.
        /// </summary>
        public string StackString { get; }

        /// <summary>
        /// Gets the stack trace.
        /// </summary>
        public JsStackTrace StackTrace { get; }
    }
}
