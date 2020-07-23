using System;

namespace VroomJs
{
    public class HostErrorInfo
    {
        internal HostErrorInfo(Exception exception = null, string name = null, string message = null)
        {
            Exception = exception;
            Name = name;
            Message = message;
        }

        public Exception Exception { get; }

        public string Name { get; }

        public string Message { get; }

        // todo: allow addition of arbitrary properties on the error object

        public JsObject ToErrorObject(JsContext context)
        {
            // If there's an exception, wrap it; otherwise just create a new object 
            JsObject errorObj;
            if(Exception != null)
            {
                errorObj = context.GetExceptionProxy(Exception);

                // If name/message were provided, they should override the default values.
                // todo: make this work
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


            //errorObj.SetPropertyValue("name", "HostError");
            //errorObj.SetPropertyValue("message", e.Message);
            //errorObj.SetPropertyValue("exceptionType", e.GetType().Name);

            // todo: should we call 'captureStackTrace' to populate the .stack property?
            // Problem is, it shows a funny thing at the top of the stack, due to error originating outside of JS
            //var global = (JsObject)context.GetGlobal();
            //var errorClass = (JsObject)global.GetPropertyValue("Error");
            //var captureStackTrace = (JsFunction)errorClass.GetPropertyValue("captureStackTrace");
            //captureStackTrace.Invoke(errorClass, errorObj, errorObj);

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
