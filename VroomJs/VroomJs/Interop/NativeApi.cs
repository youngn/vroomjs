using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;

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

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        [SuppressUnmanagedCodeSecurity]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] // todo: is this actually true?
        public static extern void js_dispose(IntPtr disposable);
        
        #endregion

        #region jsengine

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern EngineHandle jsengine_new(
            int maxYoungSpace,
            int maxOldSpace
        );

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void jsengine_terminate_execution(EngineHandle engine);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void jsengine_dump_heap_stats(EngineHandle engine);

        [DllImport(DllName)]
        public static extern int jsengine_add_template(EngineHandle engine, hostobjectcallbacks callbacks);

        #endregion

        #region jscontext

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern ContextHandle jscontext_new(int id, EngineHandle engine);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void jscontext_force_gc();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern jsvalue jscontext_execute(ContextHandle context, [MarshalAs(UnmanagedType.LPWStr)] string code, [MarshalAs(UnmanagedType.LPWStr)] string name);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern jsvalue jscontext_get_global(ContextHandle context);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern jsvalue jscontext_get_variable(ContextHandle context, [MarshalAs(UnmanagedType.LPWStr)] string name);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern jsvalue jscontext_set_variable(ContextHandle context, [MarshalAs(UnmanagedType.LPWStr)] string name, jsvalue value);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern jsvalue jscontext_new_object(ContextHandle context);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern jsvalue jscontext_new_array(ContextHandle context, int len, [In]jsvalue[] elements);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern jsvalue jscontext_get_proxy(ContextHandle context, jsvalue hostObject);

        #endregion

        #region jsobject

        [DllImport(DllName)]
        public static extern jsvalue jsobject_get_property_names(ContextHandle context, ObjectHandle obj);

        [DllImport(DllName)]
        public static extern jsvalue jsobject_get_named_property_value(ContextHandle context, ObjectHandle obj, [MarshalAs(UnmanagedType.LPWStr)] string name);

        [DllImport(DllName)]
        public static extern jsvalue jsobject_get_indexed_property_value(ContextHandle context, ObjectHandle obj, int index);

        [DllImport(DllName)]
        public static extern jsvalue jsobject_set_named_property_value(ContextHandle context, ObjectHandle obj, [MarshalAs(UnmanagedType.LPWStr)] string name, jsvalue value);

        [DllImport(DllName)]
        public static extern jsvalue jsobject_set_indexed_property_value(ContextHandle context, ObjectHandle obj, int index, jsvalue value);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern jsvalue jsfunction_invoke(ContextHandle context, ObjectHandle obj, jsvalue receiver, int argCount, [In]jsvalue[] args);

        #endregion

        #region jsscript

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern ScriptHandle jsscript_new(ContextHandle context);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern jsvalue jsscript_compile(ScriptHandle script, [MarshalAs(UnmanagedType.LPWStr)] string code,
                                                      [MarshalAs(UnmanagedType.LPWStr)] string resourceName);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern jsvalue jsscript_execute(ScriptHandle script);

        #endregion

        #region jsvalue

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void jsvalue_dispose(jsvalue value);

        #endregion

        #region jssstring

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern jsvalue jsstring_new(EngineHandle engine, [MarshalAs(UnmanagedType.LPWStr)] string value);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern int jsstring_get_value(EngineHandle engine, IntPtr str, [Out]char[] buffer);

        #endregion
    }
}
