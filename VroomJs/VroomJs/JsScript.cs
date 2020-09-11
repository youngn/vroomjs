using System;
using VroomJs.Interop;

namespace VroomJs
{
    public sealed class JsScript : V8Object<ScriptHandle>
    {
        internal JsScript(JsContext context, string code, string resourceName)
            :base(InitHandle(context), owner: context)
        {
            Context = context;

            var v = NativeApi.jsscript_compile(Handle, code, resourceName);
            Context.ExtractAndCheckReturnValue(v);
        }

        private static ScriptHandle InitHandle(JsContext context)
        {
            return NativeApi.jsscript_new(context.Handle);
        }

        public object Execute(TimeSpan? executionTimeout = null)
        {
            CheckDisposed();

            return Context.ExecuteWithTimeout(() =>
            {
                return NativeApi.jsscript_execute(Handle);
            }, executionTimeout);
        }

        internal JsContext Context { get; }
    }
}
