using System;
using System.Reflection;

namespace VroomJs
{
    public class ClrDelegateTemplate : HostObjectTemplate
    {
        public ClrDelegateTemplate()
        {
            InvokeHandler = Invoke;
        }

        internal object Invoke(IHostObjectCallbackContext context, object obj, object[] args)
        {
            var del = obj as Delegate;
            if (del == null)
                throw new InvalidOperationException("Object is not a delegate.");

            // TODO: This is pretty slow: use a cache of generated code to make it faster.
            try
            {
                return del.DynamicInvoke(args);
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
