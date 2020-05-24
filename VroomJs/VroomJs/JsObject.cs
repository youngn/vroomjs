using System;

namespace VroomJs
{
    public sealed class JsObject : JsObjectBase
    {
        public JsObject(JsContext context, IntPtr objectHandle)
            : base(context, objectHandle)
        {
        }
    }
}
