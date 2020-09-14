using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VroomJs.Interop
{
    delegate void KeepaliveRemoveDelegate(int objectId);
    delegate jsvalue KeepAliveGetPropertyValueDelegate(int objectId, [MarshalAs(UnmanagedType.LPWStr)] string name);
    delegate jsvalue KeepAliveSetPropertyValueDelegate(int objectId, [MarshalAs(UnmanagedType.LPWStr)] string name, jsvalue value);
    delegate jsvalue KeepAliveDeletePropertyDelegate(int objectId, [MarshalAs(UnmanagedType.LPWStr)] string name);
    delegate jsvalue KeepAliveEnumeratePropertiesDelegate(int objectId);
    delegate jsvalue KeepAliveInvokeDelegate(int objectId, int argCount, IntPtr args);
    delegate jsvalue KeepAliveValueOfDelegate(int objectId);
    delegate jsvalue KeepAliveToStringDelegate(int objectId);

    [StructLayout(LayoutKind.Sequential)]
    internal struct hostobjectcallbacks
    {
        public hostobjectcallbacks(
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
