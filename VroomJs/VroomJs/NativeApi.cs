using System;
using System.Runtime.InteropServices;

namespace VroomJs
{
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
            int maxYoungSpace,
            int maxOldSpace
        );

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void jsengine_terminate_execution(HandleRef engine);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void jsengine_dump_heap_stats(HandleRef engine);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void jsengine_dispose(HandleRef engine);

        [DllImport(DllName)]
        public static extern void jsengine_dispose_object(HandleRef engine, IntPtr obj);

        [DllImport(DllName)]
        public static extern int jsengine_add_template(HandleRef engine, JsCallbacks callbacks);

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
        public static extern JsValue jscontext_new_array(HandleRef context, int len, [In]JsValue[] elements);
        
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

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern JsValue jsfunction_invoke(HandleRef context, IntPtr obj, JsValue receiver, int argCount, [In]JsValue[] args);

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
        public static extern void jsvalue_dispose(JsValue value);

        #endregion

        #region jssstring

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern JsValue jsstring_new(HandleRef engine, [MarshalAs(UnmanagedType.LPWStr)] string value);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern int jsstring_get_value(HandleRef engine, IntPtr str, [Out]char[] buffer);

        #endregion
    }
}
