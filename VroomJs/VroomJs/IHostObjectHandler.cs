using System.Collections.Generic;

namespace VroomJs
{
    public interface IHostObjectHandler
    {
        // todo: rename
        void Remove(JsContext context, object obj);

        object GetPropertyValue(JsContext context, object obj, string name);

        void SetPropertyValue(JsContext context, object obj, string name, object value);

        bool DeleteProperty(JsContext context, object obj, string name);

        IEnumerable<string> EnumerateProperties(JsContext context, object obj);

        object Call(JsContext context, object obj, object[] args);

        object ValueOf(JsContext context, object obj);

        string ToString(JsContext context, object obj);
    }
}
