using System.Collections.Generic;

namespace VroomJs
{
    public class HostObjectTemplate
    {
        public delegate void RemoveDelegate(JsContext context, object obj);
        public delegate bool TryGetPropertyValueDelegate(JsContext context, object obj, string name, out object value);
        public delegate bool TrySetPropertyValueDelegate(JsContext context, object obj, string name, object value);
        public delegate bool TryDeletePropertyDelegate(JsContext context, object obj, string name, out bool deleted);
        public delegate IEnumerable<string> EnumeratePropertiesDelegate(JsContext context, object obj);
        public delegate object InvokeDelegate(JsContext context, object obj, object[] args);
        public delegate object ValueOfDelegate(JsContext context, object obj);
        public delegate string ToStringDelegate(JsContext context, object obj);

        public HostObjectTemplate(
            RemoveDelegate remove = null,
            TryGetPropertyValueDelegate getProperty = null,
            TrySetPropertyValueDelegate setProperty = null,
            TryDeletePropertyDelegate deleteProperty = null,
            EnumeratePropertiesDelegate enumerateProperties = null,
            InvokeDelegate invoke = null,
            ValueOfDelegate valueOf = null,
            ToStringDelegate toString = null
        )
        {
            RemoveHandler = remove;
            TryGetPropertyValueHandler = getProperty;
            TrySetPropertyValueHandler = setProperty;
            TryDeletePropertyHandler = deleteProperty;
            EnumeratePropertiesHandler = enumerateProperties;
            InvokeHandler = invoke;
            ValueOfHandler = valueOf;
            ToStringHandler = toString;
        }

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
