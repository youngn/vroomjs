using System;
using System.Collections.Generic;

namespace VroomJs
{
    public class HostErrorInfo
    {
        private Dictionary<string, object> _customProperties;

        internal HostErrorInfo(Exception exception = null, string name = null, string message = null)
        {
            Exception = exception;
            Name = name;
            Message = message;
        }

        public Exception Exception { get; }

        public string Name { get; }

        public string Message { get; }

        /// <summary>
        /// Gets or sets an additional property on the error object.
        /// </summary>
        /// <param name="key">The name of the property.</param>
        /// <returns>The property value.</returns>
        public object this[string key]
        {
            get
            {
                if (_customProperties == null)
                    return null;
                return _customProperties.TryGetValue(key, out object v) ? v : null;
            }
            set
            {
                if (_customProperties == null)
                    _customProperties = new Dictionary<string, object>();
                _customProperties[key] = value;
            }
        }

        public JsObject ToErrorObject(JsContext context)
        {
            // If there's an exception, wrap it; otherwise just create a new object 
            JsObject errorObj;
            if(Exception != null)
            {
                errorObj = context.GetExceptionProxy(Exception);

                // If name/message were provided, they should override the default values.
                if (!string.IsNullOrEmpty(Name))
                    errorObj["name"] = Name;
                if (!string.IsNullOrEmpty(Message))
                    errorObj["message"] = Message;
            }
            else
            {
                errorObj = context.CreateObject();
                errorObj["name"] = !string.IsNullOrEmpty(Name) ? Name : "Error";
                errorObj["message"] = !string.IsNullOrEmpty(Message) ? Message : "An unknown error occurred.";
            }

            // todo: should we call 'captureStackTrace' to populate the .stack property?
            // Problem is, it shows a funny thing at the top of the stack, due to error originating outside of JS
            //var global = (JsObject)context.GetGlobal();
            //var errorClass = (JsObject)global.GetPropertyValue("Error");
            //var captureStackTrace = (JsFunction)errorClass.GetPropertyValue("captureStackTrace");
            //captureStackTrace.Invoke(errorClass, errorObj, errorObj);

            // Copy custom properties
            if(_customProperties != null)
            {
                foreach(var kvp in _customProperties)
                {
                    errorObj[kvp.Key] = kvp.Value;
                }
            }

            return errorObj;
        }

        internal static HostErrorInfo ConvertException(Exception e)
        {
            // If this is the special HostErrorException type, the HostErrorInfo object
            // has already been built.
            if (e is HostErrorException hostErrorEx)
            {
                return hostErrorEx.ErrorInfo;
            }

            // For any other type of exception, we automatically convert it to a host error.
            return new HostErrorInfo(e);
        }
    }
}
