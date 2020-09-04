﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace VroomJs {
	public class JsEngine : IDisposable {

		// Make sure the delegates we pass to the C++ engine won't fly away during a GC.
		readonly KeepaliveRemoveDelegate _keepalive_remove;
		readonly KeepAliveGetPropertyValueDelegate _keepalive_get_property_value;
		readonly KeepAliveSetPropertyValueDelegate _keepalive_set_property_value;
		readonly KeepAliveValueOfDelegate _keepalive_valueof;
		readonly KeepAliveInvokeDelegate _keepalive_invoke;
		readonly KeepAliveDeletePropertyDelegate _keepalive_delete_property;
		readonly KeepAliveEnumeratePropertiesDelegate _keepalive_enumerate_properties;

		private readonly Dictionary<int, JsContext> _aliveContexts = new Dictionary<int, JsContext>();
		private readonly Dictionary<int, JsScript> _aliveScripts = new Dictionary<int, JsScript>();

		private int _currentContextId = 0;
		private int _currentScriptId = 0;

		public static void DumpAllocatedItems() {
			NativeApi.js_dump_allocated_items();
		}

		static JsEngine() {
		}

		public static void Initialize(string directoryPath)
		{
			NativeApi.js_initialize(directoryPath);
		}

		public static void Shutdown()
		{
			NativeApi.js_shutdown();
		}

		readonly HandleRef _engine;

		public JsEngine(int maxYoungSpace = -1, int maxOldSpace = -1) {
			_keepalive_remove = new KeepaliveRemoveDelegate(KeepAliveRemove);
			_keepalive_get_property_value = new KeepAliveGetPropertyValueDelegate(KeepAliveGetPropertyValue);
			_keepalive_set_property_value = new KeepAliveSetPropertyValueDelegate(KeepAliveSetPropertyValue);
			_keepalive_valueof = new KeepAliveValueOfDelegate(KeepAliveValueOf);
			_keepalive_invoke = new KeepAliveInvokeDelegate(KeepAliveInvoke);
			_keepalive_delete_property = new KeepAliveDeletePropertyDelegate(KeepAliveDeleteProperty);
			_keepalive_enumerate_properties = new KeepAliveEnumeratePropertiesDelegate(KeepAliveEnumerateProperties);
			
			_engine = new HandleRef(this, NativeApi.jsengine_new(
				_keepalive_remove,
				_keepalive_get_property_value,
				_keepalive_set_property_value, 
				_keepalive_valueof,
				_keepalive_invoke,
				_keepalive_delete_property,
				_keepalive_enumerate_properties,
				maxYoungSpace, 
				maxOldSpace));
		}

		public void TerminateExecution() {
			NativeApi.jsengine_terminate_execution(_engine);
		}

		public void DumpHeapStats() {
			NativeApi.jsengine_dump_heap_stats(_engine);
		}

		public void DisposeObject(IntPtr ptr) {
			// If the engine has already been explicitly disposed we pass Zero as
			// the first argument because we need to free the memory allocated by
			// "new" but not the object on the V8 heap: it has already been freed.
			if (_disposed)
				NativeApi.jsengine_dispose_object(new HandleRef(this, IntPtr.Zero), ptr);
			else
				NativeApi.jsengine_dispose_object(_engine, ptr);
		}

		private JsValue KeepAliveValueOf(int contextId, int slot) {
			JsContext context;
			if (!_aliveContexts.TryGetValue(contextId, out context)) {
				throw new Exception("fail");
			}
			return context.KeepAliveValueOf(slot);
		}
		
		private JsValue KeepAliveInvoke(int contextId, int slot, JsValue args) {
			JsContext context;
			if (!_aliveContexts.TryGetValue(contextId, out context)) {
				throw new Exception("fail");
			}
			return context.KeepAliveInvoke(slot, args);
		}

		private JsValue KeepAliveSetPropertyValue(int contextId, int slot, string name, JsValue value) {
#if DEBUG_TRACE_API
			Console.WriteLine("set prop " + contextId + " " + slot);
#endif
			JsContext context;
			if (!_aliveContexts.TryGetValue(contextId, out context)) {
				throw new Exception("fail");
			}
			return context.KeepAliveSetPropertyValue(slot, name, value);
		}

		
		private JsValue KeepAliveGetPropertyValue(int contextId, int slot, string name) {
#if DEBUG_TRACE_API
			Console.WriteLine("get prop " + contextId + " " + slot);
#endif
			JsContext context;
			if (!_aliveContexts.TryGetValue(contextId, out context)) {
				throw new Exception("fail");
			}
	
			JsValue value = context.KeepAliveGetPropertyValue(slot, name);
			return value;
		}

		private JsValue KeepAliveDeleteProperty(int contextId, int slot, string name) {
#if DEBUG_TRACE_API
			Console.WriteLine("delete prop " + contextId + " " + slot);
#endif
			JsContext context;
			if (!_aliveContexts.TryGetValue(contextId, out context)) {
				throw new Exception("fail");
			}

			JsValue value = context.KeepAliveDeleteProperty(slot, name);
			return value;
		}

		private JsValue KeepAliveEnumerateProperties(int contextId, int slot) {
#if DEBUG_TRACE_API
			Console.WriteLine("enumerate props " + contextId + " " + slot);
#endif
			JsContext context;
			if (!_aliveContexts.TryGetValue(contextId, out context)) {
				throw new Exception("fail");
			}

			JsValue value = context.KeepAliveEnumerateProperties(slot);
			return value;
		}

		private void KeepAliveRemove(int contextId, int slot) {
#if DEBUG_TRACE_API
			Console.WriteLine("Keep alive remove for " + contextId + " " + slot);
#endif
			JsContext context;
			if (!_aliveContexts.TryGetValue(contextId, out context)) {
				return;
			}
			context.KeepAliveRemove(slot);
		}

		public JsContext CreateContext() {
			CheckDisposed();
			int id = Interlocked.Increment(ref _currentContextId);
			JsContext ctx = new JsContext(id, this, _engine, ContextDisposed);
			_aliveContexts.Add(id, ctx);
			return ctx;
		}

		public JsScript CompileScript(string code, string name = "<Unamed Script>") {
			CheckDisposed();
			int id = Interlocked.Increment(ref _currentScriptId);
			JsScript script = new JsScript(id, this, _engine, new JsConvert(null), code, name, ScriptDisposed);
			_aliveScripts.Add(id, script);
			return script;
		}

		private void ContextDisposed(int id) {
			_aliveContexts.Remove(id);
		}

		private void ScriptDisposed(int id) {
			_aliveScripts.Remove(id);
		}

		#region IDisposable implementation

        bool _disposed;

        public bool IsDisposed {
            get { return _disposed; }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            CheckDisposed();

            _disposed = true;

            if (disposing) {
            	foreach (var aliveContext in _aliveContexts) {
					NativeApi.jscontext_dispose(aliveContext.Value.Handle);
            	}
				_aliveContexts.Clear();
				foreach (var aliveScript in _aliveScripts) {
					NativeApi.jsscript_dispose(aliveScript.Value.Handle);
				}
            }
#if DEBUG_TRACE_API
				Console.WriteLine("Calling jsEngine dispose: " + _engine.Handle.ToInt64());
#endif

			NativeApi.jsengine_dispose(_engine);
        }

        void CheckDisposed()
        {
            if (_disposed)
               throw new ObjectDisposedException("JsEngine:" + _engine.Handle);
        }

        ~JsEngine()
        {
            if (!_disposed)
                Dispose(false);
        }

        #endregion
	}
}
