using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace VroomJs
{
    /// <summary>
    /// A template that exposes the members (properties and methods) of a CLR object or type.
    /// </summary>
    public abstract class ClrMemberTemplate : HostObjectTemplate
    {
        internal const string JsToString = "toString";

        private readonly MissingPropertyHandling _missingPropertyHandling;

        protected ClrMemberTemplate(MissingPropertyHandling missingPropertyHandling = MissingPropertyHandling.Ignore)
        {
            _missingPropertyHandling = missingPropertyHandling;

            TryGetPropertyValueHandler = TryGetPropertyValue;
            TrySetPropertyValueHandler = TrySetPropertyValue;
            TryDeletePropertyHandler = TryDeleteProperty;
            EnumeratePropertiesHandler = EnumerateProperties;
            ToStringHandler = ToString;
        }


        protected abstract (Type, object) GetTargetTypeAndObject(object obj);

        internal bool TryGetPropertyValue(IHostObjectCallbackContext context, object obj, string name, out object value)
        {
            // Do not handle the "toString" prop - let it fall through so that the ToStringHandler gets called.
            if (name == JsToString)
            {
                value = null;
                return false;
            }

            var (type, target) = GetTargetTypeAndObject(obj);

            // TODO: This is pretty slow: use a cache of generated code to make it faster.
            try
            {
                if (TryGetMemberValue(type, target, name, out value))
                    return true;

                if (_missingPropertyHandling == MissingPropertyHandling.Throw)
                    throw new InvalidOperationException(string.Format("property not found on {0}: {1} ", type.Name, name));

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

            var (type, target) = GetTargetTypeAndObject(obj);

            // TODO: This is pretty slow: use a cache of generated code to make it faster.
            try
            {
                if (TrySetMemberValue(type, target, name, value))
                    return true;

                if (_missingPropertyHandling == MissingPropertyHandling.Throw)
                    throw new InvalidOperationException(string.Format("property not found on {0}: {1} ", type.Name, name));

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

            var (type, target) = GetTargetTypeAndObject(obj);

            if (TryGetMemberValue(type, target, name, out _))
            {
                // Members cannot be deleted
                deleted = false;
                return true;
            }

            // The member does not exist.
            if (_missingPropertyHandling == MissingPropertyHandling.Throw)
                throw new InvalidOperationException(string.Format("property not found on {0}: {1} ", type.Name, name));

            deleted = false;
            return false; //not handled
        }

        internal IEnumerable<string> EnumerateProperties(IHostObjectCallbackContext context, object obj)
        {
            var (type, target) = GetTargetTypeAndObject(obj);

            var flags = target == null
                ? BindingFlags.Public | BindingFlags.Static
                : BindingFlags.Public | BindingFlags.Instance;

            return type.GetMembers(flags)
                .Where(m =>
                {
                    // Exclude base object methods
                    if (m.DeclaringType == typeof(object))
                        return false;

                    var method = m as MethodBase;
                    return method == null || !method.IsSpecialName;
                }).Select(z => z.Name);
        }
        internal virtual string ToString(IHostObjectCallbackContext context, object obj)
        {
            var (type, _) = GetTargetTypeAndObject(obj);

            return $"[object {type.Name}]";
        }

        private bool TryGetMemberValue(Type type, object obj, string name, out object value)
        {
            var flags = obj == null
                ? BindingFlags.Public | BindingFlags.Static
                : BindingFlags.Public | BindingFlags.Instance;

            // First of all try with a public property (the most common case).
            var pi = type.GetProperty(name, flags);
            if (pi != null)
            {
                value = pi.GetValue(obj, null);
                return true;
            }

            // try field.
            var fi = type.GetField(name, flags);
            if (fi != null)
            {
                value = fi.GetValue(obj);
                return true;
            }

            // Then with an instance method: the problem is that we don't have a list of
            // parameter types so we just check if any method with the given name exists
            // and then keep alive a "weak delegate", i.e., just a name and the target.
            // The real method will be resolved during the invokation itself.
            var methodFlags = flags | BindingFlags.FlattenHierarchy;

            // TODO: This is probably slooow.
            if (type.GetMethods(methodFlags).Any(x => x.Name == name))
            {
                value = obj == null
                    ? new WeakDelegate(type, name)
                    : new WeakDelegate(obj, name);
                return true;
            }

            value = null;
            return false;
        }

        private bool TrySetMemberValue(Type type, object obj, string name, object value)
        {
            var flags = obj == null
                ? BindingFlags.Public | BindingFlags.Static
                : BindingFlags.Public | BindingFlags.Instance;

            var pi = type.GetProperty(name, flags);
            if (pi != null)
            {
                pi.SetValue(obj, value, null);
                return true;
            }

            return false;
        }
    }
}
