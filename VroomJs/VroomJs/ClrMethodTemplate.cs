using System;
using System.Reflection;

namespace VroomJs
{
    public class ClrMethodTemplate : HostObjectTemplate
    {
        public ClrMethodTemplate()
        {
            InvokeHandler = Invoke;
        }

        internal object Invoke(IHostObjectCallbackContext context, object obj, object[] args)
        {
            var func = obj as WeakDelegate;
            if (func == null)
                throw new InvalidOperationException("Object is not a method.");

            // TODO: This is pretty slow: use a cache of generated code to make it faster.
            var type = func.Target != null ? func.Target.GetType() : func.Type;
            var flags = BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.FlattenHierarchy;

            if (func.Target != null)
            {
                flags |= BindingFlags.Instance;
            }
            else
            {
                flags |= BindingFlags.Static;
            }

            if (obj is BoundWeakDelegate)
            {
                flags |= BindingFlags.NonPublic;
            }

            try
            {
                return type.InvokeMember(func.MethodName, flags, null, func.Target, args);
            }
            catch (TargetInvocationException e)
            {
                // Client code probably isn't interested in the exception part related to
                // reflection, so we unwrap it and pass to V8 only the real exception thrown.
                if (e.InnerException != null)
                    throw e.InnerException;
                throw;
            }
        }
    }
}
