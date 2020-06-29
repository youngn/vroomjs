using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace VroomJs
{
    partial class JsEngine
    {
        internal class HostObjectTemplateRegistration
        {
            private readonly JsEngine _engine;
            private readonly Predicate<object> _selector;
            private readonly HostObjectTemplate _template;

            // Make sure the delegates we pass to the C++ engine won't fly away during a GC.
            private readonly NativeHostObjectCallbacks _nativeCallbacks;

            public HostObjectTemplateRegistration(
                JsEngine engine, HostObjectTemplate template, Predicate<object> selector = null)
            {
                _engine = engine;
                _selector = selector;
                _template = template;

                // todo: use discretion about which callbacks to supply
                _nativeCallbacks = new NativeHostObjectCallbacks(
                                Remove,
                                GetPropertyValue,
                                SetPropertyValue,
                                DeleteProperty,
                                EnumerateProperties,
                                Invoke,
                                ValueOf,
                                ToString);

                Id = NativeApi.jsengine_add_template(engine.Handle, _nativeCallbacks);
            }

            public int Id { get; }

            public bool IsApplicableTo(object obj)
            {
                return _selector == null || _selector(obj);
            }

            private void Remove(int contextId, int objectId)
            {
                var context = _engine.GetContext(contextId);

                if (_template.Remove != null)
                {
                    var obj = context.GetHostObject(objectId);
                    _template.Remove(context, obj);
                }

                context.RemoveHostObject(objectId);
            }

            private JsValue GetPropertyValue(int contextId, int objectId, string name)
            {
                var context = _engine.GetContext(contextId);
                var obj = context.GetHostObject(objectId);

                try
                {
                    var result = _template.GetPropertyValue(context, obj, name);

                    return JsValue.ForValue(result, context);
                }
                catch (Exception e)
                {
                    return JsValue.ForClrError(context.AddHostObject(e));
                }
            }

            private JsValue SetPropertyValue(int contextId, int objectId, string name, JsValue value)
            {
                var context = _engine.GetContext(contextId);
                var obj = context.GetHostObject(objectId);
                try
                {
                    _template.SetPropertyValue(context, obj, name, value.Extract(context));

                    // The actual value that we set here isn't important, it just has to be
                    // something other than Empty in order to indicate that we've handled it.
                    return JsValue.ForNull();
                }
                catch (Exception e)
                {
                    return JsValue.ForClrError(context.AddHostObject(e));
                }
            }

            private JsValue DeleteProperty(int contextId, int objectId, string name)
            {
                var context = _engine.GetContext(contextId);
                var obj = context.GetHostObject(objectId);
                try
                {
                    var result = _template.DeleteProperty(context, obj, name);

                    return JsValue.ForBoolean(result);
                }
                catch (Exception e)
                {
                    return JsValue.ForClrError(context.AddHostObject(e));
                }
            }

            private JsValue EnumerateProperties(int contextId, int objectId)
            {
                var context = _engine.GetContext(contextId);
                var obj = context.GetHostObject(objectId);
                try
                {
                    var result = _template.EnumerateProperties(context, obj);

                    // todo: clean this up
                    var values = result.Select(z => JsValue.ForValue(z, context)).ToArray();
                    return NativeApi.jscontext_new_array(context.Handle, values.Length, values);
                }
                catch (Exception e)
                {
                    return JsValue.ForClrError(context.AddHostObject(e));
                }
            }

            private JsValue Invoke(int contextId, int objectId, int argCount, IntPtr args)
            {
                var context = _engine.GetContext(contextId);
                var obj = context.GetHostObject(objectId);

                var stepSize = Marshal.SizeOf<JsValue>();
                var arguments = Enumerable.Range(0, argCount)
                    .Select(i => Marshal.PtrToStructure<JsValue>(new IntPtr(args.ToInt64() + (stepSize * i))).Extract(context))
                    .ToArray();

                try
                {
                    var result = _template.Invoke(context, obj, arguments);

                    return JsValue.ForValue(result, context);
                }
                catch (Exception e)
                {
                    return JsValue.ForClrError(context.AddHostObject(e));
                }
            }

            private JsValue ValueOf(int contextId, int objectId)
            {
                var context = _engine.GetContext(contextId);
                var obj = context.GetHostObject(objectId);

                try
                {
                    var result = _template.ValueOf(context, obj);

                    return JsValue.ForValue(result, context);
                }
                catch (Exception e)
                {
                    return JsValue.ForClrError(context.AddHostObject(e));
                }
            }

            private JsValue ToString(int contextId, int objectId)
            {
                var context = _engine.GetContext(contextId);
                var obj = context.GetHostObject(objectId);

                try
                {
                    var result = _template.ToString(context, obj);

                    return JsValue.ForValue(result, context);
                }
                catch (Exception e)
                {
                    return JsValue.ForClrError(context.AddHostObject(e));
                }
            }
        }
    }
}
