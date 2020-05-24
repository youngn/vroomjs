using System;
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

    static class NativeApi
    {
        private const string DllName = "VroomJsNative";

        #region global

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern void js_initialize([MarshalAs(UnmanagedType.LPStr)] string directoryPath);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void js_shutdown();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void js_dump_allocated_items();

        #endregion

        #region jsengine

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr jsengine_new(
            KeepaliveRemoveDelegate keepaliveRemove,
            KeepAliveGetPropertyValueDelegate keepaliveGetPropertyValue,
            KeepAliveSetPropertyValueDelegate keepaliveSetPropertyValue,
            KeepAliveValueOfDelegate keepaliveValueOf,
            KeepAliveInvokeDelegate keepaliveInvoke,
            KeepAliveDeletePropertyDelegate keepaliveDeleteProperty,
            KeepAliveEnumeratePropertiesDelegate keepaliveEnumerateProperties,
            int maxYoungSpace, int maxOldSpace
        );

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void jsengine_terminate_execution(HandleRef engine);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void jsengine_dump_heap_stats(HandleRef engine);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void jsengine_dispose(HandleRef engine);

        [DllImport(DllName)]
        public static extern void jsengine_dispose_object(HandleRef engine, IntPtr obj);

        #endregion

        #region jscontext

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr jscontext_new(int id, HandleRef engine);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void jscontext_dispose(HandleRef context);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void jscontext_force_gc();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern JsValue jscontext_execute(HandleRef context, [MarshalAs(UnmanagedType.LPWStr)] string str, [MarshalAs(UnmanagedType.LPWStr)] string name);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern JsValue jscontext_execute_script(HandleRef context, HandleRef script);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern JsValue jscontext_get_global(HandleRef context);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern JsValue jscontext_get_variable(HandleRef context, [MarshalAs(UnmanagedType.LPWStr)] string name);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern JsValue jscontext_set_variable(HandleRef context, [MarshalAs(UnmanagedType.LPWStr)] string name, JsValue value);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern JsValue jscontext_invoke(HandleRef context, IntPtr funcPtr, IntPtr thisPtr, JsValue args);

        #endregion

        #region jsobject

        [DllImport(DllName)]
        public static extern JsValue jsobject_get_property_names(HandleRef context, IntPtr obj);

        [DllImport(DllName)]
        public static extern JsValue jsobject_get_named_property_value(HandleRef context, IntPtr obj, [MarshalAs(UnmanagedType.LPWStr)] string name);

        [DllImport(DllName)]
        public static extern JsValue jsobject_get_indexed_property_value(HandleRef context, IntPtr obj, int index);

        [DllImport(DllName)]
        public static extern JsValue jsobject_set_named_property_value(HandleRef context, IntPtr obj, [MarshalAs(UnmanagedType.LPWStr)] string name, JsValue value);

        [DllImport(DllName)]
        public static extern JsValue jsobject_set_indexed_property_value(HandleRef context, IntPtr obj, int index, JsValue value);

        [DllImport(DllName)]
        public static extern JsValue jsobject_invoke_property(HandleRef context, IntPtr obj, [MarshalAs(UnmanagedType.LPWStr)] string name, JsValue args);

        #endregion

        #region jsscript

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr jsscript_new(HandleRef engine);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern JsValue jsscript_compile(HandleRef script, [MarshalAs(UnmanagedType.LPWStr)] string str,
                                                      [MarshalAs(UnmanagedType.LPWStr)] string name);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr jsscript_dispose(HandleRef script);

        #endregion

        #region jsvalue

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern JsValue jsvalue_alloc_string([MarshalAs(UnmanagedType.LPWStr)] string str);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern JsValue jsvalue_alloc_array(int length);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void jsvalue_dispose(JsValue value);

        #endregion
    }
}
