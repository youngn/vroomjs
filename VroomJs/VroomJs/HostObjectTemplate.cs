using System.Collections.Generic;

namespace VroomJs
{
    public class HostObjectTemplate
    {
        public delegate void RemoveDelegate(JsContext context, object obj);
        public delegate object GetPropertyValueDelegate(JsContext context, object obj, string name);
        public delegate void SetPropertyValueDelegate(JsContext context, object obj, string name, object value);
        public delegate bool DeletePropertyDelegate(JsContext context, object obj, string name);
        public delegate IEnumerable<string> EnumeratePropertiesDelegate(JsContext context, object obj);
        public delegate object InvokeDelegate(JsContext context, object obj, object[] args);
        public delegate object ValueOfDelegate(JsContext context, object obj);
        public delegate string ToStringDelegate(JsContext context, object obj);

        public HostObjectTemplate(
            RemoveDelegate remove = null,
            GetPropertyValueDelegate getProperty = null,
            SetPropertyValueDelegate setProperty = null,
            DeletePropertyDelegate deleteProperty = null,
            EnumeratePropertiesDelegate enumerateProperties = null,
            InvokeDelegate invoke = null,
            ValueOfDelegate valueOf = null,
            ToStringDelegate toString = null
        )
        {
            RemoveHandler = remove;
            GetPropertyValueHandler = getProperty;
            SetPropertyValueHandler = setProperty;
            DeletePropertyHandler = deleteProperty;
            EnumeratePropertiesHandler = enumerateProperties;
            InvokeHandler = invoke;
            ValueOfHandler = valueOf;
            ToStringHandler = toString;
        }

        public RemoveDelegate RemoveHandler;
        public GetPropertyValueDelegate GetPropertyValueHandler;
        public SetPropertyValueDelegate SetPropertyValueHandler;
        public DeletePropertyDelegate DeletePropertyHandler;
        public EnumeratePropertiesDelegate EnumeratePropertiesHandler;
        public InvokeDelegate InvokeHandler;
        public ValueOfDelegate ValueOfHandler;
        public ToStringDelegate ToStringHandler;
    }
}
