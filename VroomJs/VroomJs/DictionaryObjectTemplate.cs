using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace VroomJs
{
    public class DictionaryObjectTemplate : HostObjectTemplate
    {
        public DictionaryObjectTemplate()
        {
            GetPropertyValueHandler = GetPropertyValue;
            SetPropertyValueHandler = SetPropertyValue;
            DeletePropertyHandler = DeleteProperty;
            EnumeratePropertiesHandler = EnumerateProperties;
        }

        private object GetPropertyValue(JsContext context, object obj, string name)
        {
            // we need to fall back to the prototype verison we set up because v8 won't call an object as a function, it needs
            // to be from a proper FunctionTemplate.
            //if (!string.IsNullOrEmpty(name) && name.Equals("valueOf"))
            //{
            //    return JsValue.ForEmpty();
            //}
            //if (!string.IsNullOrEmpty(name) && name.Equals("toString"))
            //{
            //    return JsValue.ForEmpty();
            //}

            try
            {
                if (!string.IsNullOrEmpty(name))
                {
                    var upperCamelCase = char.ToUpper(name[0]) + name.Substring(1);
                    object value;
                    if (TryGetMemberValue(obj, upperCamelCase, out value))
                    {
                        return value;
                    }
                    if (TryGetMemberValue(obj, name, out value))
                    {
                        return value;
                    }
                }

                // Else an error.
                throw new InvalidOperationException(string.Format("key not found in dictionary: {0} ", name));
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

        private void SetPropertyValue(JsContext context, object obj, string name, object value)
        {
            try
            {
                if (!string.IsNullOrEmpty(name))
                {
                    var upperCamelCase = char.ToUpper(name[0]) + name.Substring(1);
                    if (TrySetMemberValue(obj, upperCamelCase, value))
                    {
                        return;
                    }
                    if (TrySetMemberValue(obj, name, value))
                    {
                        return;
                    }
                }

                throw new InvalidOperationException(string.Format("key not found in dictionary: {0} ", name));
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

        private bool DeleteProperty(JsContext context, object obj, string name)
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

        private IEnumerable<string> EnumerateProperties(JsContext context, object obj)
        {
            // TODO: This is pretty slow: use a cache of generated code to make it faster.
            if (typeof(IDictionary).IsAssignableFrom(obj.GetType()))
            {
                IDictionary dictionary = (IDictionary)obj;
                return dictionary.Keys.Cast<string>();
            }

            throw new InvalidOperationException("Object is not a dictionary.");
        }

        private bool TryGetMemberValue(object obj, string name, out object value)
        {
            if (obj is IDictionary)
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

            value = null;
            return false;
        }

        private bool TrySetMemberValue(object obj, string name, object value)
        {
            if (obj is IDictionary)
            {
                IDictionary dictionary = (IDictionary)obj;
                dictionary[name] = value;
                return true;
            }

            return false;
        }
    }
}
