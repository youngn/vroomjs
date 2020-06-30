using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace VroomJs
{
    public class ClrObjectTemplate : HostObjectTemplate
    {
        internal const string JsToString = "toString";

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

        internal bool TryGetPropertyValue(IHostObjectCallbackContext context, object obj, string name, out object value)
        {
            // we need to fall back to the prototype verison we set up because v8 won't call an object as a function, it needs
            // to be from a proper FunctionTemplate.
            //if (!string.IsNullOrEmpty(name) && name.Equals("valueOf"))
            //{
            //    return JsValue.ForEmpty();
            //}

            // Do not handle the "toString" prop - let it fall through so that the ToStringHandler gets called.
            if (name == JsToString)
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
                if (TryGetMemberValue(type, obj, name, out value))
                    return true;

                if (MissingPropertyHandling == MissingPropertyHandling.Throw)
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

        internal bool TrySetPropertyValue(IHostObjectCallbackContext context, object obj, string name, object value)
        {
            // Do not handle the "toString" prop - let it fall through, to be consistent with
            // TryGetPropertyValue behaviour.
            if (name == JsToString)
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
                if (TrySetMemberValue(type, obj, name, value))
                    return true;

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

        internal bool TryDeleteProperty(IHostObjectCallbackContext context, object obj, string name, out bool deleted)
        {
            // Do not handle the "toString" prop - let it fall through, to be consistent with
            // TryGetPropertyValue behaviour.
            if (name == JsToString)
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

            if (TryGetMemberValue(type, obj, name, out _))
            {
                // Members cannot be deleted
                deleted = false;
                return true;
            }

            // The member does not exist.
            if (MissingPropertyHandling == MissingPropertyHandling.Throw)
                throw new InvalidOperationException(string.Format("property not found on {0}: {1} ", type, name));

            deleted = false;
            return false; //not handled
        }

        internal IEnumerable<string> EnumerateProperties(IHostObjectCallbackContext context, object obj)
        {
            return obj.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance)
                .Where(m =>
                {
                    // Exclude base object methods
                    if (m.DeclaringType == typeof(object))
                        return false;

                    var method = m as MethodBase;
                    return method == null || !method.IsSpecialName;
                }).Select(z => z.Name);
        }

        internal string ToString(IHostObjectCallbackContext context, object obj)
        {
            if (UseNetToString)
                return obj.ToString();

            return $"[object {obj.GetType().Name}]";
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
            PropertyInfo pi = type.GetProperty(name, flags);
            if (pi != null)
            {
                value = pi.GetValue(obj, null);
                return true;
            }

            // try field.
            FieldInfo fi = type.GetField(name, flags);
            if (fi != null)
            {
                value = fi.GetValue(obj);
                return true;
            }

            // Then with an instance method: the problem is that we don't have a list of
            // parameter types so we just check if any method with the given name exists
            // and then keep alive a "weak delegate", i.e., just a name and the target.
            // The real method will be resolved during the invokation itself.
            BindingFlags mFlags = flags | BindingFlags.FlattenHierarchy;

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

            PropertyInfo pi = type.GetProperty(name, flags);
            if (pi != null)
            {
                pi.SetValue(obj, value, null);
                return true;
            }

            return false;
        }
    }
}
