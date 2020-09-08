using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using VroomJs.Interop;


namespace VroomJs
{
    public partial class JsEngine : IDisposable
    {
        public static void Initialize()
        {
            var location = Assembly.GetAssembly(typeof(JsEngine)).Location;
            NativeApi.js_initialize(location);
        }

        public static void Shutdown()
        {
            NativeApi.js_shutdown();
        }

        public static void DumpAllocatedItems()
        {
            NativeApi.js_dump_allocated_items();
        }

        private readonly List<HostObjectTemplateRegistration> _templateRegistrations
            = new List<HostObjectTemplateRegistration>();
        private readonly HostObjectTemplateRegistration _exceptionTemplateRegistration;

        private readonly EngineHandle _handle;
        private readonly Dictionary<int, JsContext> _aliveContexts = new Dictionary<int, JsContext>();
        private bool _disposed;

        private int _currentContextId = 0;

        public JsEngine(EngineConfiguration configuration = null)
        {
            var memoryConfig = configuration?.Memory ?? new EngineConfiguration.MemoryConfiguration();

            _handle = NativeApi.jsengine_new(
                memoryConfig.MaxYoungSpace,
                memoryConfig.MaxOldSpace);

            _exceptionTemplateRegistration = new HostObjectTemplateRegistration(this, new ExceptionTemplate());

            if (configuration != null)
            {
                configuration.Apply(this);
            }
        }

        public JsContext CreateContext()
        {
            CheckDisposed();

            var id = Interlocked.Increment(ref _currentContextId);
            var ctx = new JsContext(id, this, ContextDisposed);
            _aliveContexts.Add(id, ctx);

            return ctx;
        }

        public void DumpHeapStats()
        {
            NativeApi.jsengine_dump_heap_stats(_handle);
        }

        internal EngineHandle Handle => _handle;

        internal int ExceptionTemplateId => _exceptionTemplateRegistration.Id;

        internal HostErrorFilterDelegate HostErrorFilter { get; set; }

        internal void RegisterHostObjectTemplate(HostObjectTemplate template, Predicate<object> selector = null)
        {
            _templateRegistrations.Add(new HostObjectTemplateRegistration(this, template, selector));
        }

        internal void DisposeObject(IntPtr ptr)
        {
            // If the engine has already been explicitly disposed we pass Zero as
            // the first argument because we need to free the memory allocated by
            // "new" but not the object on the V8 heap: it has already been freed.
            if (_disposed)
                NativeApi.jsengine_dispose_object(EngineHandle.CreateInvalid(), ptr);
            else
                NativeApi.jsengine_dispose_object(_handle, ptr);
        }

        internal JsContext GetContext(int id)
        {
            if (!_aliveContexts.TryGetValue(id, out JsContext context))
                throw new InvalidOperationException($"Invalid context ID: {id}");

            return context;
        }

        internal int SelectTemplate(object obj)
        {
            var template = _templateRegistrations.FirstOrDefault(r => r.IsApplicableTo(obj));
            return template != null ? template.Id : -1;
        }

        internal void TerminateExecution()
        {
            NativeApi.jsengine_terminate_execution(_handle);
        }

        private void ContextDisposed(int id)
        {
            _aliveContexts.Remove(id);
        }

        #region IDisposable implementation

        public bool IsDisposed
        {
            get { return _disposed; }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            foreach (var context in _aliveContexts.Values)
            {
                context.Dispose();
            }
            _aliveContexts.Clear();

            _handle.Dispose();
        }

        #endregion

        internal void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(JsEngine));
        }
    }
}
