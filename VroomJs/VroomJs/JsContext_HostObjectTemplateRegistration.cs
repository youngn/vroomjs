using System;
using System.Linq;
using System.Runtime.InteropServices;
using VroomJs.Interop;

namespace VroomJs
{
    partial class JsContext
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

            private readonly JsContext _context;
            private readonly Predicate<object> _selector;
            private readonly HostObjectTemplate _template;
            private readonly CallbackContext _callbackContext;

            // Make sure the delegates we pass to the C++ engine won't fly away during a GC.
            private readonly hostobjectcallbacks _nativeCallbacks;

            public HostObjectTemplateRegistration(
                JsContext context, HostObjectTemplate template, Predicate<object> selector = null)
            {
                _context = context;
                _selector = selector;
                _template = template;
                _callbackContext = new CallbackContext(context);

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

                Id = NativeApi.jscontext_add_template(context.Handle, _nativeCallbacks);
            }

            public int Id { get; }

            public bool IsApplicableTo(object obj)
            {
                return _selector == null || _selector(obj);
            }

            private void Remove(int contextId, int objectId)
            {
                if (_template.RemoveHandler != null)
                {
                    var obj = _context.GetHostObject(objectId);
                    _template.RemoveHandler(obj);
                }

                _context.RemoveHostObject(objectId);
            }

            private jsvalue GetPropertyValue(int contextId, int objectId, string name)
            {
                var obj = _context.GetHostObject(objectId);

                try
                {
                    if (_template.TryGetPropertyValueHandler(_callbackContext, obj, name, out object value))
                        return JsValue.ForValue(value, _context);

                    return JsValue.ForEmpty(); // not handled
                }
                catch (Exception e)
                {
                    return ConvertException(e, _callbackContext);
                }
            }

            private jsvalue SetPropertyValue(int contextId, int objectId, string name, jsvalue value)
            {
                var obj = _context.GetHostObject(objectId);

                try
                {
                    if(_template.TrySetPropertyValueHandler(_callbackContext, obj, name, ((JsValue)value).Extract(_context)))
                    {
                        // The actual value that we set here isn't important, it just has to be
                        // something other than Empty in order to indicate that we've handled it.
                        return JsValue.ForNull();
                    }

                    return JsValue.ForEmpty(); // not handled
                }
                catch (Exception e)
                {
                    return ConvertException(e, _callbackContext);
                }
            }

            private jsvalue DeleteProperty(int contextId, int objectId, string name)
            {
                var obj = _context.GetHostObject(objectId);

                try
                {
                    if(_template.TryDeletePropertyHandler(_callbackContext, obj, name, out bool deleted))
                        return JsValue.ForBoolean(deleted);

                    return JsValue.ForEmpty(); // not handled
                }
                catch (Exception e)
                {
                    return ConvertException(e, _callbackContext);
                }
            }

            private jsvalue EnumerateProperties(int contextId, int objectId)
            {
                var obj = _context.GetHostObject(objectId);

                jsvalue[] propNames;
                try
                {
                    var result = _template.EnumeratePropertiesHandler(_callbackContext, obj);
                    propNames = result.Select(z => (jsvalue)JsValue.ForValue(z, _context)).ToArray();
                }
                catch (Exception e)
                {
                    return ConvertException(e, _callbackContext);
                }

                return NativeApi.jscontext_new_array(_context.Handle, propNames.Length, propNames);
            }

            private jsvalue Invoke(int contextId, int objectId, int argCount, IntPtr args)
            {
                var obj = _context.GetHostObject(objectId);

                var stepSize = Marshal.SizeOf<JsValue>(); // todo: save this
                var arguments = Enumerable.Range(0, argCount)
                    .Select(i => Marshal.PtrToStructure<JsValue>(new IntPtr(args.ToInt64() + (stepSize * i))).Extract(_context))
                    .ToArray();

                try
                {
                    var result = _template.InvokeHandler(_callbackContext, obj, arguments);

                    return JsValue.ForValue(result, _context);
                }
                catch (Exception e)
                {
                    return ConvertException(e, _callbackContext);
                }
            }

            private jsvalue ValueOf(int contextId, int objectId)
            {
                var obj = _context.GetHostObject(objectId);

                try
                {
                    var result = _template.ValueOfHandler(_callbackContext, obj);

                    return JsValue.ForValue(result, _context);
                }
                catch (Exception e)
                {
                    return ConvertException(e, _callbackContext);
                }
            }

            private jsvalue ToString(int contextId, int objectId)
            {
                var obj = _context.GetHostObject(objectId);

                try
                {
                    var result = _template.ToStringHandler(_callbackContext, obj);

                    return JsValue.ForValue(result, _context);
                }
                catch (Exception e)
                {
                    return ConvertException(e, _callbackContext);
                }
            }

            private JsValue ConvertException(Exception e, CallbackContext callbackContext)
            {
                var errorInfo = HostErrorInfo.ConvertException(e);
                var raiseError = _context.HostErrorFilter?.Invoke(callbackContext, errorInfo) ?? true;
                if(raiseError)
                    return JsValue.ForHostError(errorInfo, _context);

                // The error was suppressed by the filter, so script execution must be terminated,
                // and the CLR exception re-thrown after termination.
                _context.Engine.TerminateExecution();
                _context.SetPendingException(errorInfo.Exception ?? e);

                return JsValue.ForEmpty();
            }
        }
    }
}
