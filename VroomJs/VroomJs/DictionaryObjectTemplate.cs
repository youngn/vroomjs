using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace VroomJs
{
    public class DictionaryObjectTemplate : HostObjectTemplate
    {
        public DictionaryObjectTemplate()
        {
            TryGetPropertyValueHandler = TryGetPropertyValue;
            TrySetPropertyValueHandler = TrySetPropertyValue;
            TryDeletePropertyHandler = TryDeleteProperty;
            EnumeratePropertiesHandler = EnumerateProperties;
        }

        public MissingPropertyHandling MissingPropertyHandling { get; set; }

        private bool TryGetPropertyValue(JsContext context, object obj, string name, out object value)
        {
            // todo: can 'name' be null?
            var upperCamelCase = char.ToUpper(name[0]) + name.Substring(1);
            if (TryGetMemberValue(obj, upperCamelCase, out value))
            {
                return true;
            }
            if (TryGetMemberValue(obj, name, out value))
            {
                return true;
            }

            if (MissingPropertyHandling == MissingPropertyHandling.Throw)
                throw new InvalidOperationException(string.Format("key not found in dictionary: {0} ", name));

            value = null;
            return false; // not handled
        }

        private bool TrySetPropertyValue(JsContext context, object obj, string name, object value)
        {
            // todo: can 'name' be null?
            SetMemberValue(obj, name, value);
            return true;
        }

        private bool TryDeleteProperty(JsContext context, object obj, string name, out bool deleted)
        {
            // TODO: This is pretty slow: use a cache of generated code to make it faster.
            if (obj is IDictionary dictionary)
            {
                if (dictionary.Contains(name))
                {
                    dictionary.Remove(name);
                    deleted = true;
                    return true;
                }
            }

            throw new InvalidOperationException("Object is not a dictionary.");
        }

        private IEnumerable<string> EnumerateProperties(JsContext context, object obj)
        {
            if (obj is IDictionary dictionary)
            {
                return dictionary.Keys.Cast<string>();
            }

            throw new InvalidOperationException("Object is not a dictionary.");
        }

        private bool TryGetMemberValue(object obj, string name, out object value)
        {
            if (obj is IDictionary dictionary)
            {
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

        private void SetMemberValue(object obj, string name, object value)
        {
            if (obj is IDictionary dictionary)
            {
                dictionary[name] = value;
            }

            throw new InvalidOperationException("Object is not a dictionary.");
        }
    }
}
