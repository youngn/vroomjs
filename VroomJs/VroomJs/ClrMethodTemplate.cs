using System;
using System.Linq;
using System.Reflection;

namespace VroomJs.VroomJs
{
    public class ClrMethodTemplate : HostObjectTemplate
    {
        public ClrMethodTemplate()
        {
            InvokeHandler = Invoke;
        }

        private object Invoke(JsContext context, object obj, object[] args)
        {
            // TODO: This is pretty slow: use a cache of generated code to make it faster.

            Type constructorType = obj as Type;
            if (constructorType != null)
            {
                return Activator.CreateInstance(constructorType, args);
            }

            WeakDelegate func = obj as WeakDelegate;
            if (func == null)
            {
                throw new Exception("not a function.");
            }

            Type type = func.Target != null ? func.Target.GetType() : func.Type;
            BindingFlags flags = BindingFlags.Public
                    | BindingFlags.InvokeMethod | BindingFlags.FlattenHierarchy;

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

            // need to convert methods from JsFunction's into delegates?
            if (args.Any(z => z != null && z.GetType() == typeof(JsFunction)))
            {
                CheckAndResolveJsFunctions(type, func.MethodName, flags, args);
            }

            try
            {
                object result = type.InvokeMember(func.MethodName, flags, null, func.Target, args);
                return result;
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

        private static void CheckAndResolveJsFunctions(Type type, string methodName, BindingFlags flags, object[] args)
        {
            MethodInfo mi = type.GetMethod(methodName, flags);
            ParameterInfo[] paramTypes = mi.GetParameters();

            for (int i = 0; i < args.Length; i++)
            {
                if (i >= paramTypes.Length)
                {
                    continue;
                }
                if (args[i] != null && args[i].GetType() == typeof(JsFunction))
                {
                    JsFunction function = (JsFunction)args[i];
                    args[i] = function.MakeDelegate(paramTypes[i].ParameterType);
                }
            }
        }
    }
}
