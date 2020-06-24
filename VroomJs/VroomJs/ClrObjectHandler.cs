using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace VroomJs
{
    public class ClrObjectHandler : IHostObjectHandler
    {
        public void Remove(JsContext context, object obj)
        {
        }

        public object GetPropertyValue(JsContext context, object obj, string name)
        {
            // we need to fall back to the prototype verison we set up because v8 won't call an object as a function, it needs
            // to be from a proper FunctionTemplate.
            if (!string.IsNullOrEmpty(name) && name.Equals("valueOf"))
            {
                return JsValue.ForEmpty();
            }
            if (!string.IsNullOrEmpty(name) && name.Equals("toString"))
            {
                return JsValue.ForEmpty();
            }

            // TODO: This is pretty slow: use a cache of generated code to make it faster.
            Type type;
            if (obj is Type)
            {
                type = (Type)obj;
            }
            else
            {
                type = obj.GetType();
            }
            try
            {
                if (!string.IsNullOrEmpty(name))
                {
                    var upperCamelCase = char.ToUpper(name[0]) + name.Substring(1);
                    object value;
                    if (TryGetMemberValue(type, obj, upperCamelCase, out value))
                    {
                        return value;
                    }
                    if (TryGetMemberValue(type, obj, name, out value))
                    {
                        return value;
                    }
                }

                // Else an error.
                throw new InvalidOperationException(string.Format("property not found on {0}: {1} ", type, name));
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

        public void SetPropertyValue(JsContext context, object obj, string name, object value)
        {
            // TODO: This is pretty slow: use a cache of generated code to make it faster.
            Type type;
            if (obj is Type)
            {
                type = (Type)obj;
            }
            else
            {
                type = obj.GetType();
            }

            try
            {
                if (!string.IsNullOrEmpty(name))
                {
                    var upperCamelCase = char.ToUpper(name[0]) + name.Substring(1);
                    if (TrySetMemberValue(type, obj, upperCamelCase, value))
                    {
                        return;
                    }
                    if (TrySetMemberValue(type, obj, name, value))
                    {
                        return;
                    }
                }

                throw new InvalidOperationException(string.Format("property not found on {0}: {1} ", type, name));
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

        public bool DeleteProperty(JsContext context, object obj, string name)
        {
            // TODO: This is pretty slow: use a cache of generated code to make it faster.
            if (typeof(IDictionary).IsAssignableFrom(obj.GetType()))
            {
                IDictionary dictionary = (IDictionary)obj;
                if (dictionary.Contains(name))
                {
                    dictionary.Remove(name);
                    return true;
                }
            }
            return false;
        }

        public IEnumerable<string> EnumerateProperties(JsContext context, object obj)
        {
            // TODO: This is pretty slow: use a cache of generated code to make it faster.
            if (typeof(IDictionary).IsAssignableFrom(obj.GetType()))
            {
                IDictionary dictionary = (IDictionary)obj;
                return dictionary.Keys.Cast<string>();
            }

            return obj.GetType().GetMembers(
                BindingFlags.Public |
                BindingFlags.Instance).Where(m =>
                {
                    var method = m as MethodBase;
                    return method == null || !method.IsSpecialName;
                }).Select(z => z.Name);
        }

        public object Call(JsContext context, object obj, object[] args)
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

        public object ValueOf(JsContext context, object obj)
        {
            Type type = obj.GetType();
            MethodInfo mi = type.GetMethod("valueOf") ?? type.GetMethod("ValueOf");
            if (mi != null)
            {
                object result = mi.Invoke(obj, new object[0]);
                return result;
            }
            return obj;
        }

        public string ToString(JsContext context, object obj)
        {
            Type type = obj.GetType();
            MethodInfo mi = type.GetMethod("toString") ?? type.GetMethod("ToString");
            if (mi != null)
            {
                object result = mi.Invoke(obj, new object[0]);
                return result.ToString();
            }
            return obj.ToString();
        }

        internal bool TryGetMemberValue(Type type, object obj, string name, out object value)
        {
            // dictionaries.
            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                IDictionary dictionary = (IDictionary)obj;
                if (dictionary.Contains(name))
                {
                    value = dictionary[name];
                }
                else
                {
                    value = null;
                }
                return true;
            }

            BindingFlags flags;
            if (type == obj)
            {
                flags = BindingFlags.Public | BindingFlags.Static;
            }
            else
            {
                flags = BindingFlags.Public | BindingFlags.Instance;
            }

            // First of all try with a public property (the most common case).
            PropertyInfo pi = type.GetProperty(name, flags | BindingFlags.GetProperty);
            if (pi != null)
            {
                value = pi.GetValue(obj, null);
                return true;
            }

            // try field.
            FieldInfo fi = type.GetField(name, flags | BindingFlags.GetProperty);
            if (fi != null)
            {
                value = fi.GetValue(obj);
                return true;
            }

            // Then with an instance method: the problem is that we don't have a list of
            // parameter types so we just check if any method with the given name exists
            // and then keep alive a "weak delegate", i.e., just a name and the target.
            // The real method will be resolved during the invokation itself.
            BindingFlags mFlags = flags | BindingFlags.InvokeMethod | BindingFlags.FlattenHierarchy;

            // TODO: This is probably slooow.
            if (type.GetMethods(mFlags).Any(x => x.Name == name))
            {
                if (type == obj)
                {
                    value = new WeakDelegate(type, name);
                }
                else
                {
                    value = new WeakDelegate(obj, name);
                }
                return true;
            }

            value = null;
            return false;
        }

        internal bool TrySetMemberValue(Type type, object obj, string name, object value)
        {
            // dictionaries.
            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                IDictionary dictionary = (IDictionary)obj;
                dictionary[name] = value;
                return true;
            }

            BindingFlags flags;
            if (type == obj)
            {
                flags = BindingFlags.Public | BindingFlags.Static;
            }
            else
            {
                flags = BindingFlags.Public | BindingFlags.Instance;
            }

            PropertyInfo pi = type.GetProperty(name, flags | BindingFlags.SetProperty);
            if (pi != null)
            {
                pi.SetValue(obj, value, null);
                return true;
            }

            return false;
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
