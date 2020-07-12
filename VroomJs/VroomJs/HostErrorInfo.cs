using System;

namespace VroomJs
{
    internal class HostErrorInfo
    {
        public HostErrorInfo(JsObject errorObject)
        {
            if (errorObject == null)
                throw new ArgumentNullException(nameof(errorObject));

            ErrorObject = errorObject;
        }

        public JsObject ErrorObject { get; }
    }
}
