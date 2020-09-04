using System;
using System.Collections.Generic;


namespace VroomJs
{
    internal class ExceptionTemplate : HostObjectTemplate
    {
        private const string DefaultErrorName = "Error";

        static class PropertyNames
        {
            public const string Name = "name";
            public const string Message = "message";
        }

        static class ExceptionDataKeys
        {
            public const string Name = nameof(VroomJs) + "." + nameof(ExceptionTemplate) + ".ErrorName";
            public const string Message = nameof(VroomJs) + "." + nameof(ExceptionTemplate) + ".ErrorMessage";
        }

        public ExceptionTemplate()
        {
            TryGetPropertyValueHandler = TryGetPropertyValue;
            TrySetPropertyValueHandler = TrySetPropertyValue;
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
                    value = GetErrorName(ex);
                    return true;

                case PropertyNames.Message:
                    value = GetErrorMessage(ex);
                    return true;

                default:
                    value = null;
                    return false;
            }
        }

        private bool TrySetPropertyValue(IHostObjectCallbackContext context, object obj, string name, object value)
        {
            var ex = obj as Exception;
            if (ex == null)
                throw new InvalidOperationException("Object is not an exception.");

            switch (name)
            {
                case PropertyNames.Name:
                    ex.Data[ExceptionDataKeys.Name] = value;
                    return true;

                case PropertyNames.Message:
                    ex.Data[ExceptionDataKeys.Message] = value;
                    return true;

                default:
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
        }

        private string ToString(IHostObjectCallbackContext context, object obj)
        {
            var ex = obj as Exception;
            if (ex == null)
                throw new InvalidOperationException("Object is not an exception.");

            return $"{GetErrorName(ex)}: {GetErrorMessage(ex)}";
        }

        private static object GetErrorName(Exception ex)
        {
            return ex.Data[ExceptionDataKeys.Name] ?? DefaultErrorName;
        }

        private static object GetErrorMessage(Exception ex)
        {
            return ex.Data[ExceptionDataKeys.Message] ?? ex.Message;
        }
    }
}
