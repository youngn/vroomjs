using System;
using System.Collections;
using System.Collections.Generic;

namespace VroomJs
{
    public class JsArray : JsObject, IEnumerable<object>
    {
        internal JsArray(JsContext context, ObjectHandle objectHandle)
            : base(context, objectHandle)
        {
        }

        public int GetLength()
        {
            // todo: is there a more direct way to do this?
            return (int)GetPropertyValue("length");
        }

        public IEnumerator<object> GetEnumerator()
        {
            var length = GetLength();
            for (var i = 0; i < length; i++)
                yield return GetPropertyValue(i);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
