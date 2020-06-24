using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VroomJs
{
    delegate void KeepaliveRemoveDelegate(int contextId, int objectId);
    delegate JsValue KeepAliveGetPropertyValueDelegate(int contextId, int objectId, [MarshalAs(UnmanagedType.LPWStr)] string name);
    delegate JsValue KeepAliveSetPropertyValueDelegate(int contextId, int objectId, [MarshalAs(UnmanagedType.LPWStr)] string name, JsValue value);
    delegate JsValue KeepAliveDeletePropertyDelegate(int contextId, int objectId, [MarshalAs(UnmanagedType.LPWStr)] string name);
    delegate JsValue KeepAliveEnumeratePropertiesDelegate(int contextId, int objectId);
    delegate JsValue KeepAliveInvokeDelegate(int contextId, int objectId, int argCount, IntPtr args);
    delegate JsValue KeepAliveValueOfDelegate(int contextId, int objectId);
    delegate JsValue KeepAliveToStringDelegate(int contextId, int objectId);

    [StructLayout(LayoutKind.Sequential)]
    struct NativeHostObjectCallbacks
    {
        public NativeHostObjectCallbacks(
            KeepaliveRemoveDelegate remove,
            KeepAliveGetPropertyValueDelegate getPropertyValue = null,
            KeepAliveSetPropertyValueDelegate setPropertyValue = null,
            KeepAliveDeletePropertyDelegate deleteProperty = null,
            KeepAliveEnumeratePropertiesDelegate enumerateProperties = null,
            KeepAliveInvokeDelegate invoke = null,
            KeepAliveValueOfDelegate valueOf = null,
            KeepAliveToStringDelegate toString = null)
        {
            Debug.Assert(remove != null);

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
