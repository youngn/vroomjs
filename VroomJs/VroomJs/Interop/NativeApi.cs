﻿using System;
using System.Runtime.InteropServices;

namespace VroomJs.Interop
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
        public static extern int jsengine_add_template(HandleRef engine, hostobjectcallbacks callbacks);

        #endregion

        #region jscontext

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr jscontext_new(int id, HandleRef engine);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void jscontext_dispose(HandleRef context);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void jscontext_force_gc();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern jsvalue jscontext_execute(HandleRef context, [MarshalAs(UnmanagedType.LPWStr)] string str, [MarshalAs(UnmanagedType.LPWStr)] string name);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern jsvalue jscontext_execute_script(HandleRef context, HandleRef script);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern jsvalue jscontext_get_global(HandleRef context);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern jsvalue jscontext_get_variable(HandleRef context, [MarshalAs(UnmanagedType.LPWStr)] string name);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern jsvalue jscontext_set_variable(HandleRef context, [MarshalAs(UnmanagedType.LPWStr)] string name, jsvalue value);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern jsvalue jscontext_new_object(HandleRef context);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern jsvalue jscontext_new_array(HandleRef context, int len, [In]jsvalue[] elements);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern jsvalue jscontext_get_proxy(HandleRef context, jsvalue hostObject);

        #endregion

        #region jsobject

        [DllImport(DllName)]
        public static extern jsvalue jsobject_get_property_names(HandleRef context, IntPtr obj);

        [DllImport(DllName)]
        public static extern jsvalue jsobject_get_named_property_value(HandleRef context, IntPtr obj, [MarshalAs(UnmanagedType.LPWStr)] string name);

        [DllImport(DllName)]
        public static extern jsvalue jsobject_get_indexed_property_value(HandleRef context, IntPtr obj, int index);

        [DllImport(DllName)]
        public static extern jsvalue jsobject_set_named_property_value(HandleRef context, IntPtr obj, [MarshalAs(UnmanagedType.LPWStr)] string name, jsvalue value);

        [DllImport(DllName)]
        public static extern jsvalue jsobject_set_indexed_property_value(HandleRef context, IntPtr obj, int index, jsvalue value);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern jsvalue jsfunction_invoke(HandleRef context, IntPtr obj, jsvalue receiver, int argCount, [In]jsvalue[] args);

        #endregion

        #region jsscript

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr jsscript_new(HandleRef context);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern jsvalue jsscript_compile(HandleRef script, [MarshalAs(UnmanagedType.LPWStr)] string str,
                                                      [MarshalAs(UnmanagedType.LPWStr)] string resourceName);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr jsscript_dispose(HandleRef script);

        #endregion

        #region jsvalue

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void jsvalue_dispose(jsvalue value);

        #endregion

        #region jssstring

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern jsvalue jsstring_new(HandleRef engine, [MarshalAs(UnmanagedType.LPWStr)] string value);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern int jsstring_get_value(HandleRef engine, IntPtr str, [Out]char[] buffer);

        #endregion
    }
}
