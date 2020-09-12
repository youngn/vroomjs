using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using VroomJs.Interop;


namespace VroomJs
{
    public partial class JsEngine : V8Object<EngineHandle>
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

        private readonly List<HostObjectTemplateRegistration> _templateRegistrations
            = new List<HostObjectTemplateRegistration>();
        private readonly HostObjectTemplateRegistration _exceptionTemplateRegistration;

        private readonly Dictionary<int, JsContext> _aliveContexts = new Dictionary<int, JsContext>();

        private int _currentContextId = 0;

        public JsEngine(EngineConfiguration configuration = null)
            :base(InitHandle(configuration))
        {
            _exceptionTemplateRegistration = new HostObjectTemplateRegistration(this, new ExceptionTemplate());

            if (configuration != null)
            {
                configuration.Apply(this);
            }
        }

        private static EngineHandle InitHandle(EngineConfiguration configuration)
        {
            var memoryConfig = configuration?.Memory ?? new EngineConfiguration.MemoryConfiguration();

            return NativeApi.jsengine_new(
                memoryConfig.MaxYoungSpace,
                memoryConfig.MaxOldSpace);
        }

        public JsContext CreateContext()
        {
            CheckDisposed();

            var id = Interlocked.Increment(ref _currentContextId);
            var ctx = new JsContext(id, this);
            _aliveContexts.Add(id, ctx);

            return ctx;
        }

        public void DumpHeapStats()
        {
            NativeApi.jsengine_dump_heap_stats(Handle);
        }

        internal int ExceptionTemplateId => _exceptionTemplateRegistration.Id;

        internal HostErrorFilterDelegate HostErrorFilter { get; set; }

        internal void RegisterHostObjectTemplate(HostObjectTemplate template, Predicate<object> selector = null)
        {
            _templateRegistrations.Add(new HostObjectTemplateRegistration(this, template, selector));
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
            NativeApi.jsengine_terminate_execution(Handle);
        }

        protected override void DisposeCore()
        {
            _aliveContexts.Clear();

            base.DisposeCore();
        }

        protected override void OwnedObjectDisposed(V8Object ownedObject)
        {
            var context = (JsContext)ownedObject;
            _aliveContexts.Remove(context.Id);

            base.OwnedObjectDisposed(ownedObject);
        }
    }
}
