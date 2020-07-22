﻿using System;
using System.Collections.Generic;
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

                }
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

                // Only supply the native callback if the template has a handler defined.
                // (This is the entire motivation for using a set of delegates as opposed to defining an interface.)
                _nativeCallbacks = new hostobjectcallbacks(
                    Remove, // always set, because JsContext must be notified
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
                var obj = context.GetHostObject(objectId);

                try
                {
                    if (_template.TryGetPropertyValueHandler(new CallbackContext(context), obj, name, out object value))
                        return JsValue.ForValue(value, context);

                    return JsValue.ForEmpty(); // not handled
                }
                catch (Exception e)
                {
                    return ConvertException(e, context);
                }
            }

            private jsvalue SetPropertyValue(int contextId, int objectId, string name, jsvalue value)
            {
                var context = _engine.GetContext(contextId);
                var obj = context.GetHostObject(objectId);

                try
                {
                    if(_template.TrySetPropertyValueHandler(new CallbackContext(context), obj, name, ((JsValue)value).Extract(context)))
                    {
                        // The actual value that we set here isn't important, it just has to be
                        // something other than Empty in order to indicate that we've handled it.
                        return JsValue.ForNull();
                    }

                    return JsValue.ForEmpty(); // not handled
                }
                catch (Exception e)
                {
                    return ConvertException(e, context);
                }
            }

            private jsvalue DeleteProperty(int contextId, int objectId, string name)
            {
                var context = _engine.GetContext(contextId);
                var obj = context.GetHostObject(objectId);

                try
                {
                    if(_template.TryDeletePropertyHandler(new CallbackContext(context), obj, name, out bool deleted))
                        return JsValue.ForBoolean(deleted);

                    return JsValue.ForEmpty(); // not handled
                }
                catch (Exception e)
                {
                    return ConvertException(e, context);
                }
            }

            private jsvalue EnumerateProperties(int contextId, int objectId)
            {
                var context = _engine.GetContext(contextId);
                var obj = context.GetHostObject(objectId);

                jsvalue[] propNames;
                try
                {
                    var result = _template.EnumeratePropertiesHandler(new CallbackContext(context), obj);
                    propNames = result.Select(z => (jsvalue)JsValue.ForValue(z, context)).ToArray();
                }
                catch (Exception e)
                {
                    return ConvertException(e, context);
                }

                return NativeApi.jscontext_new_array(context.Handle, propNames.Length, propNames);
            }

            private jsvalue Invoke(int contextId, int objectId, int argCount, IntPtr args)
            {
                var context = _engine.GetContext(contextId);
                var obj = context.GetHostObject(objectId);

                var stepSize = Marshal.SizeOf<JsValue>();
                var arguments = Enumerable.Range(0, argCount)
                    .Select(i => Marshal.PtrToStructure<JsValue>(new IntPtr(args.ToInt64() + (stepSize * i))).Extract(context))
                    .ToArray();

                try
                {
                    var result = _template.InvokeHandler(new CallbackContext(context), obj, arguments);

                    return JsValue.ForValue(result, context);
                }
                catch (Exception e)
                {
                    return ConvertException(e, context);
                }
            }

            private jsvalue ValueOf(int contextId, int objectId)
            {
                var context = _engine.GetContext(contextId);
                var obj = context.GetHostObject(objectId);

                try
                {
                    var result = _template.ValueOfHandler(new CallbackContext(context), obj);

                    return JsValue.ForValue(result, context);
                }
                catch (Exception e)
                {
                    return ConvertException(e, context);
                }
            }

            private jsvalue ToString(int contextId, int objectId)
            {
                var context = _engine.GetContext(contextId);
                var obj = context.GetHostObject(objectId);

                try
                {
                    var result = _template.ToStringHandler(new CallbackContext(context), obj);

                    return JsValue.ForValue(result, context);
                }
                catch (Exception e)
                {
                    return ConvertException(e, context);
                }
            }

            private JsValue ConvertException(Exception e, JsContext context)
            {
                // Wrap the Exception in a JS proxy object
                var x = JsValue.ForHostObject(context.AddHostObject(e), _engine._exceptionTemplateRegistration.Id);
                var v = (JsValue)NativeApi.jscontext_get_proxy(context.Handle, x);
                var errorObj = (JsObject)v.Extract(context);

                // todo: should we call 'captureStackTrace' to populate the .stack property?
                // Problem is, it shows a funny thing at the top of the stack, due to error originating outside of JS
                //var global = (JsObject)context.GetGlobal();
                //var errorClass = (JsObject)global.GetPropertyValue("Error");
                //var captureStackTrace = (JsFunction)errorClass.GetPropertyValue("captureStackTrace");
                //captureStackTrace.Invoke(errorClass, errorObj, errorObj);

                //errorObj.SetPropertyValue("name", "HostError");
                //errorObj.SetPropertyValue("message", e.Message);
                //errorObj.SetPropertyValue("exceptionType", e.GetType().Name);

                return JsValue.ForHostError(new HostErrorInfo(errorObj));
            }
        }
    }
}
