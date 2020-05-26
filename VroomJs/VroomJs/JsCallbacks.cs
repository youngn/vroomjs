using System.Runtime.InteropServices;

namespace VroomJs
{
    delegate void KeepaliveRemoveDelegate(int context, int slot);
    delegate JsValue KeepAliveGetPropertyValueDelegate(int context, int slot, [MarshalAs(UnmanagedType.LPWStr)] string name);
    delegate JsValue KeepAliveSetPropertyValueDelegate(int context, int slot, [MarshalAs(UnmanagedType.LPWStr)] string name, JsValue value);
    delegate JsValue KeepAliveValueOfDelegate(int context, int slot);
    delegate JsValue KeepAliveInvokeDelegate(int context, int slot, JsValue args);
    delegate JsValue KeepAliveDeletePropertyDelegate(int context, int slot, [MarshalAs(UnmanagedType.LPWStr)] string name);
    delegate JsValue KeepAliveEnumeratePropertiesDelegate(int context, int slot);

    [StructLayout(LayoutKind.Sequential)]
    struct JsCallbacks
    {
        public JsCallbacks(
            KeepaliveRemoveDelegate remove = null,
            KeepAliveGetPropertyValueDelegate getPropertyValue = null,
            KeepAliveSetPropertyValueDelegate setPropertyValue = null,
            KeepAliveValueOfDelegate valueOf = null,
            KeepAliveInvokeDelegate invoke = null,
            KeepAliveDeletePropertyDelegate deleteProperty = null,
            KeepAliveEnumeratePropertiesDelegate enumerateProperties = null)
        {
            Remove = remove;
            GetPropertyValue = getPropertyValue;
            SetPropertyValue = setPropertyValue;
            ValueOf = valueOf;
            Invoke = invoke;
            DeleteProperty = deleteProperty;
            EnumerateProperties = enumerateProperties;
        }

        public readonly KeepaliveRemoveDelegate Remove;
        public readonly KeepAliveGetPropertyValueDelegate GetPropertyValue;
        public readonly KeepAliveSetPropertyValueDelegate SetPropertyValue;
        public readonly KeepAliveValueOfDelegate ValueOf;
        public readonly KeepAliveInvokeDelegate Invoke;
        public readonly KeepAliveDeletePropertyDelegate DeleteProperty;
        public readonly KeepAliveEnumeratePropertiesDelegate EnumerateProperties;
    };
}
