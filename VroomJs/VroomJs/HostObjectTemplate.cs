using System.Collections.Generic;

namespace VroomJs
{
    public interface IHostObjectCallbackContext
    {

    }

    public class HostObjectTemplate
    {
        public delegate void RemoveDelegate(object obj);
        public delegate bool TryGetPropertyValueDelegate(IHostObjectCallbackContext context, object obj, string name, out object value);
        public delegate bool TrySetPropertyValueDelegate(IHostObjectCallbackContext context, object obj, string name, object value);
        public delegate bool TryDeletePropertyDelegate(IHostObjectCallbackContext context, object obj, string name, out bool deleted);
        public delegate IEnumerable<string> EnumeratePropertiesDelegate(IHostObjectCallbackContext context, object obj);
        public delegate object InvokeDelegate(IHostObjectCallbackContext context, object obj, object[] args);
        public delegate object ValueOfDelegate(IHostObjectCallbackContext context, object obj);
        public delegate string ToStringDelegate(IHostObjectCallbackContext context, object obj);

        public HostObjectTemplate(
            RemoveDelegate remove = null,
            TryGetPropertyValueDelegate tryGetProperty = null,
            TrySetPropertyValueDelegate trySetProperty = null,
            TryDeletePropertyDelegate tryDeleteProperty = null,
            EnumeratePropertiesDelegate enumerateProperties = null,
            InvokeDelegate invoke = null,
            ValueOfDelegate valueOf = null,
            ToStringDelegate toString = null
        )
        {
            RemoveHandler = remove;
            TryGetPropertyValueHandler = tryGetProperty;
            TrySetPropertyValueHandler = trySetProperty;
            TryDeletePropertyHandler = tryDeleteProperty;
            EnumeratePropertiesHandler = enumerateProperties;
            InvokeHandler = invoke;
            ValueOfHandler = valueOf;
            ToStringHandler = toString;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Must not throw. Any exception that escapes this handler may crash the process.
        /// </remarks>
        public RemoveDelegate RemoveHandler;
        public TryGetPropertyValueDelegate TryGetPropertyValueHandler;
        public TrySetPropertyValueDelegate TrySetPropertyValueHandler;
        public TryDeletePropertyDelegate TryDeletePropertyHandler;
        public EnumeratePropertiesDelegate EnumeratePropertiesHandler;
        public InvokeDelegate InvokeHandler;
        public ValueOfDelegate ValueOfHandler;
        public ToStringDelegate ToStringHandler;
    }
}
