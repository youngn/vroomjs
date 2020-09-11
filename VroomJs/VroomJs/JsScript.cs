using System;
using VroomJs.Interop;

namespace VroomJs
{
    public sealed class JsScript : V8Object<ScriptHandle>
    {
        internal JsScript(JsContext context, ScriptHandle handle)
            :base(handle, owner: context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
        }

        public object Execute(TimeSpan? executionTimeout = null)
        {
            CheckDisposed();

            return Context.ExecuteWithTimeout(() =>
            {
                return NativeApi.jsscript_execute(Handle);
            }, executionTimeout);
        }

        public JsContext Context => (JsContext)Owner;
    }
}
