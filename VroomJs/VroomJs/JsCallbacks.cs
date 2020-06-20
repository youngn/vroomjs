using System;
using System.Runtime.InteropServices;

namespace VroomJs
{
    delegate void KeepaliveRemoveDelegate(int context, int slot);
    delegate JsValue KeepAliveGetPropertyValueDelegate(int context, int slot, [MarshalAs(UnmanagedType.LPWStr)] string name);
    delegate JsValue KeepAliveSetPropertyValueDelegate(int context, int slot, [MarshalAs(UnmanagedType.LPWStr)] string name, JsValue value);
    delegate JsValue KeepAliveDeletePropertyDelegate(int context, int slot, [MarshalAs(UnmanagedType.LPWStr)] string name);
    delegate JsValue KeepAliveEnumeratePropertiesDelegate(int context, int slot);
    delegate JsValue KeepAliveInvokeDelegate(int context, int slot, int argCount, IntPtr args);
    delegate JsValue KeepAliveValueOfDelegate(int context, int slot);
    delegate JsValue KeepAliveToStringDelegate(int context, int slot);

    [StructLayout(LayoutKind.Sequential)]
    struct JsCallbacks
    {
        public JsCallbacks(
            KeepaliveRemoveDelegate remove = null,
            KeepAliveGetPropertyValueDelegate getPropertyValue = null,
            KeepAliveSetPropertyValueDelegate setPropertyValue = null,
            KeepAliveDeletePropertyDelegate deleteProperty = null,
            KeepAliveEnumeratePropertiesDelegate enumerateProperties = null,
            KeepAliveInvokeDelegate invoke = null,
            KeepAliveValueOfDelegate valueOf = null,
            KeepAliveToStringDelegate toString = null)
        {
            Remove = remove;
            GetPropertyValue = getPropertyValue;
            SetPropertyValue = setPropertyValue;
            ValueOf = valueOf;
            Invoke = invoke;
            DeleteProperty = deleteProperty;
            EnumerateProperties = enumerateProperties;
            ToStringCallback = toString;
        }

        public readonly KeepaliveRemoveDelegate Remove;
        public readonly KeepAliveGetPropertyValueDelegate GetPropertyValue;
        public readonly KeepAliveSetPropertyValueDelegate SetPropertyValue;
        public readonly KeepAliveDeletePropertyDelegate DeleteProperty;
        public readonly KeepAliveEnumeratePropertiesDelegate EnumerateProperties;
        public readonly KeepAliveInvokeDelegate Invoke;
        public readonly KeepAliveValueOfDelegate ValueOf;
        public readonly KeepAliveToStringDelegate ToStringCallback;
    };
}
