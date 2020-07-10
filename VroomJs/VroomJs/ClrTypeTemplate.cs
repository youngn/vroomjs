using System;
using System.Reflection;

namespace VroomJs
{
    public class ClrTypeTemplate : ClrMemberTemplate
    {
        public ClrTypeTemplate(bool allowInvokeConstructor = false,
            MissingPropertyHandling missingPropertyHandling = MissingPropertyHandling.Ignore)
            :base(missingPropertyHandling)
        {
            if(allowInvokeConstructor)
                InvokeHandler = InvokeConstructor;
        }

        protected override (Type, object) GetTargetTypeAndObject(object obj)
        {
            var type = obj as Type;
            if (type == null)
                throw new InvalidOperationException("Object is not a Type.");

            return (type, null);
        }

        internal object InvokeConstructor(IHostObjectCallbackContext context, object obj, object[] args)
        {
            var type = obj as Type;
            if (type == null)
                throw new InvalidOperationException("Object is not a Type.");

            // TODO: This is pretty slow: use a cache of generated code to make it faster.

            try
            {
                return Activator.CreateInstance(type, args);
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
