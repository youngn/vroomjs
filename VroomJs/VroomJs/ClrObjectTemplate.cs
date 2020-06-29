using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace VroomJs
{
    public class ClrObjectTemplate : HostObjectTemplate
    {
        public ClrObjectTemplate()
        {
            TryGetPropertyValueHandler = TryGetPropertyValue;
            TrySetPropertyValueHandler = TrySetPropertyValue;
            TryDeletePropertyHandler = TryDeleteProperty;
            EnumeratePropertiesHandler = EnumerateProperties;
            ToStringHandler = ToString;
        }

        public MissingPropertyHandling MissingPropertyHandling { get; set; }

        public bool UseNetToString { get; set; }

        private bool TryGetPropertyValue(JsContext context, object obj, string name, out object value)
        {
            // we need to fall back to the prototype verison we set up because v8 won't call an object as a function, it needs
            // to be from a proper FunctionTemplate.
            //if (!string.IsNullOrEmpty(name) && name.Equals("valueOf"))
            //{
            //    return JsValue.ForEmpty();
            //}

            // Do not handle the "toString" prop - let it fall through so that the ToStringHandler gets called.
            if (!string.IsNullOrEmpty(name) && name.Equals("toString"))
            {
                value = null;
                return false;
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
                    if (TryGetMemberValue(type, obj, upperCamelCase, out value))
                    {
                        return true;
                    }
                    if (TryGetMemberValue(type, obj, name, out value))
                    {
                        return true;
                    }
                }

                if(MissingPropertyHandling == MissingPropertyHandling.Throw)
                    throw new InvalidOperationException(string.Format("property not found on {0}: {1} ", type, name));

                value = null;
                return false; //not handled
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

        private bool TrySetPropertyValue(JsContext context, object obj, string name, object value)
        {
            // Do not handle the "toString" prop - let it fall through, to be consistent with
            // TryGetPropertyValue behaviour.
            if (!string.IsNullOrEmpty(name) && name.Equals("toString"))
                return false;

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
                        return true;
                    }
                    if (TrySetMemberValue(type, obj, name, value))
                    {
                        return true;
                    }
                }

                if (MissingPropertyHandling == MissingPropertyHandling.Throw)
                    throw new InvalidOperationException(string.Format("property not found on {0}: {1} ", type, name));

                return false; //not handled
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

        private bool TryDeleteProperty(JsContext context, object obj, string name, out bool deleted)
        {
            // Do not handle the "toString" prop - let it fall through, to be consistent with
            // TryGetPropertyValue behaviour.
            if (!string.IsNullOrEmpty(name) && name.Equals("toString"))
            {
                deleted = false;
                return false;
            }

            Type type;
            if (obj is Type)
            {
                type = (Type)obj;
            }
            else
            {
                type = obj.GetType();
            }

            if (!string.IsNullOrEmpty(name))
            {
                var upperCamelCase = char.ToUpper(name[0]) + name.Substring(1);
                if (TryGetMemberValue(type, obj, upperCamelCase, out _)
                    || TryGetMemberValue(type, obj, name, out _))
                {
                    // Members cannot be deleted
                    deleted = false;
                    return false;
                }
            }

            // The member does not exist.
            if (MissingPropertyHandling == MissingPropertyHandling.Throw)
                throw new InvalidOperationException(string.Format("property not found on {0}: {1} ", type, name));

            deleted = false;
            return false; //not handled
        }

        private IEnumerable<string> EnumerateProperties(JsContext context, object obj)
        {
            return obj.GetType().GetMembers(
                BindingFlags.Public |
                BindingFlags.Instance).Where(m =>
                {
                    var method = m as MethodBase;
                    return method == null || !method.IsSpecialName;
                }).Select(z => z.Name);
        }

        //private object ValueOf(JsContext context, object obj)
        //{
        //    Type type = obj.GetType();
        //    MethodInfo mi = type.GetMethod("valueOf") ?? type.GetMethod("ValueOf");
        //    if (mi != null)
        //    {
        //        object result = mi.Invoke(obj, new object[0]);
        //        return result;
        //    }
        //    return obj;
        //}

        private string ToString(JsContext context, object obj)
        {
            if (UseNetToString)
                return obj.ToString();

            return $"[object {obj.GetType()}]";
        }

        private bool TryGetMemberValue(Type type, object obj, string name, out object value)
        {
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

        private bool TrySetMemberValue(Type type, object obj, string name, object value)
        {
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
    }
}
