﻿using System;
using System.Linq;
using System.Runtime.InteropServices;
using VroomJs.Interop;

namespace VroomJs
{
    partial class JsEngine
    {
        internal class HostObjectTemplateRegistration
        {
            class CallbackContext : IHostObjectCallbackContext
            {
                public CallbackContext(JsContext context)
                {
                    Context = context;
                }

                public JsContext Context { get; }
            }

            private readonly JsEngine _engine;
            private readonly Predicate<object> _selector;
            private readonly HostObjectTemplate _template;

            // Make sure the delegates we pass to the C++ engine won't fly away during a GC.
            private readonly hostobjectcallbacks _nativeCallbacks;

            public HostObjectTemplateRegistration(
                JsEngine engine, HostObjectTemplate template, Predicate<object> selector = null)
            {
                _engine = engine;
                _selector = selector;
                _template = template;

                // Only supply the native callback if the template has a handler defined, so that we avoid
                // incurring any performance overhead for something that is not used.
                // (This is the entire motivation for using a set of delegates as opposed to defining an interface.)
                _nativeCallbacks = new hostobjectcallbacks(
                    Remove, // always set, because JsContext must be notified in order to clean-up the keep-alive store
                    template.TryGetPropertyValueHandler != null ? GetPropertyValue : (KeepAliveGetPropertyValueDelegate)null,
                    template.TrySetPropertyValueHandler != null ? SetPropertyValue : (KeepAliveSetPropertyValueDelegate)null,
                    template.TryDeletePropertyHandler != null ? DeleteProperty : (KeepAliveDeletePropertyDelegate)null,
                    template.EnumeratePropertiesHandler != null ? EnumerateProperties : (KeepAliveEnumeratePropertiesDelegate)null,
                    template.InvokeHandler != null ? Invoke : (KeepAliveInvokeDelegate)null,
                    template.ValueOfHandler != null ? ValueOf : (KeepAliveValueOfDelegate)null,
                    template.ToStringHandler != null ? ToString : (KeepAliveToStringDelegate)null
                );

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

                if (_template.RemoveHandler != null)
                {
                    var obj = context.GetHostObject(objectId);
                    _template.RemoveHandler(obj);
                }

                context.RemoveHostObject(objectId);
            }

            private jsvalue GetPropertyValue(int contextId, int objectId, string name)
            {
                var context = _engine.GetContext(contextId);
                var callbackContext = new CallbackContext(context);
                var obj = context.GetHostObject(objectId);

                try
                {
                    if (_template.TryGetPropertyValueHandler(callbackContext, obj, name, out object value))
                        return JsValue.ForValue(value, context);

                    return JsValue.ForEmpty(); // not handled
                }
                catch (Exception e)
                {
                    return ConvertException(e, callbackContext);
                }
            }

            private jsvalue SetPropertyValue(int contextId, int objectId, string name, jsvalue value)
            {
                var context = _engine.GetContext(contextId);
                var callbackContext = new CallbackContext(context);
                var obj = context.GetHostObject(objectId);

                try
                {
                    if(_template.TrySetPropertyValueHandler(callbackContext, obj, name, ((JsValue)value).Extract(context)))
                    {
                        // The actual value that we set here isn't important, it just has to be
                        // something other than Empty in order to indicate that we've handled it.
                        return JsValue.ForNull();
                    }

                    return JsValue.ForEmpty(); // not handled
                }
                catch (Exception e)
                {
                    return ConvertException(e, callbackContext);
                }
            }

            private jsvalue DeleteProperty(int contextId, int objectId, string name)
            {
                var context = _engine.GetContext(contextId);
                var callbackContext = new CallbackContext(context);
                var obj = context.GetHostObject(objectId);

                try
                {
                    if(_template.TryDeletePropertyHandler(callbackContext, obj, name, out bool deleted))
                        return JsValue.ForBoolean(deleted);

                    return JsValue.ForEmpty(); // not handled
                }
                catch (Exception e)
                {
                    return ConvertException(e, callbackContext);
                }
            }

            private jsvalue EnumerateProperties(int contextId, int objectId)
            {
                var context = _engine.GetContext(contextId);
                var callbackContext = new CallbackContext(context);
                var obj = context.GetHostObject(objectId);

                jsvalue[] propNames;
                try
                {
                    var result = _template.EnumeratePropertiesHandler(callbackContext, obj);
                    propNames = result.Select(z => (jsvalue)JsValue.ForValue(z, context)).ToArray();
                }
                catch (Exception e)
                {
                    return ConvertException(e, callbackContext);
                }

                return NativeApi.jscontext_new_array(context.Handle, propNames.Length, propNames);
            }

            private jsvalue Invoke(int contextId, int objectId, int argCount, IntPtr args)
            {
                var context = _engine.GetContext(contextId);
                var callbackContext = new CallbackContext(context);
                var obj = context.GetHostObject(objectId);

                var stepSize = Marshal.SizeOf<JsValue>();
                var arguments = Enumerable.Range(0, argCount)
                    .Select(i => Marshal.PtrToStructure<JsValue>(new IntPtr(args.ToInt64() + (stepSize * i))).Extract(context))
                    .ToArray();

                try
                {
                    var result = _template.InvokeHandler(callbackContext, obj, arguments);

                    return JsValue.ForValue(result, context);
                }
                catch (Exception e)
                {
                    return ConvertException(e, callbackContext);
                }
            }

            private jsvalue ValueOf(int contextId, int objectId)
            {
                var context = _engine.GetContext(contextId);
                var callbackContext = new CallbackContext(context);
                var obj = context.GetHostObject(objectId);

                try
                {
                    var result = _template.ValueOfHandler(callbackContext, obj);

                    return JsValue.ForValue(result, context);
                }
                catch (Exception e)
                {
                    return ConvertException(e, callbackContext);
                }
            }

            private jsvalue ToString(int contextId, int objectId)
            {
                var context = _engine.GetContext(contextId);
                var callbackContext = new CallbackContext(context);
                var obj = context.GetHostObject(objectId);

                try
                {
                    var result = _template.ToStringHandler(callbackContext, obj);

                    return JsValue.ForValue(result, context);
                }
                catch (Exception e)
                {
                    return ConvertException(e, callbackContext);
                }
            }

            private JsValue ConvertException(Exception e, CallbackContext callbackContext)
            {
                var errorInfo = HostErrorInfo.ConvertException(e);
                _engine.HostErrorFilter?.Invoke(callbackContext, errorInfo);
                return JsValue.ForHostError(errorInfo, callbackContext.Context);
            }
        }
    }
}
