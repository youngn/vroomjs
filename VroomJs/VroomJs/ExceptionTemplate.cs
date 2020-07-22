using System;
using System.Collections.Generic;


namespace VroomJs
{
    internal class ExceptionTemplate : HostObjectTemplate
    {
        private const string ErrorName = "HostError";

        static class PropertyNames
        {
            public const string Name = "name";
            public const string Message = "message";
            public const string ExceptionType = "exceptionType";
        }

        public ExceptionTemplate()
        {
            TryGetPropertyValueHandler = TryGetPropertyValue;
            EnumeratePropertiesHandler = EnumerateProperties;
            ToStringHandler = ToString;
        }

        private bool TryGetPropertyValue(IHostObjectCallbackContext context, object obj, string name, out object value)
        {
            var ex = obj as Exception;
            if (ex == null)
                throw new InvalidOperationException("Object is not an exception.");

            switch (name)
            {
                case PropertyNames.Name:
                    value = ErrorName;
                    return true;

                case PropertyNames.Message:
                    value = ex.Message;
                    return true;

                case PropertyNames.ExceptionType:
                    value = ex.GetType().Name;
                    return true;

                default:
                    value = null;
                    return false;
            }
        }

        private IEnumerable<string> EnumerateProperties(IHostObjectCallbackContext context, object obj)
        {
            var ex = obj as Exception;
            if (ex == null)
                throw new InvalidOperationException("Object is not an exception.");

            yield return PropertyNames.Name;
            yield return PropertyNames.Message;
            yield return PropertyNames.ExceptionType;
        }

        private string ToString(IHostObjectCallbackContext context, object obj)
        {
            var ex = obj as Exception;
            if (ex == null)
                throw new InvalidOperationException("Object is not an exception.");

            return $"{ErrorName}: {ex.Message}";
        }
    }
}
