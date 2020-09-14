using System;
using System.Collections.Generic;
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


        private readonly Dictionary<int, JsContext> _aliveContexts = new Dictionary<int, JsContext>();

        private int _currentContextId = 0;

        public JsEngine(EngineConfiguration configuration = null)
            :base(InitHandle(configuration))
        {
            configuration?.Apply(this);
        }

        private static EngineHandle InitHandle(EngineConfiguration configuration)
        {
            var memoryConfig = configuration?.Memory ?? new EngineConfiguration.MemoryConfiguration();

            return NativeApi.jsengine_new(
                memoryConfig.MaxYoungSpace,
                memoryConfig.MaxOldSpace);
        }

        public JsContext CreateContext(ContextConfiguration configuration = null)
        {
            CheckDisposed();

            var ctx = new JsContext(this, configuration);
            //_aliveContexts.Add(0, ctx);

            return ctx;
        }

        public void DumpHeapStats()
        {
            NativeApi.jsengine_dump_heap_stats(Handle);
        }

        internal JsContext GetContext(int id)
        {
            if (!_aliveContexts.TryGetValue(id, out JsContext context))
                throw new InvalidOperationException($"Invalid context ID: {id}");

            return context;
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
            //_aliveContexts.Remove(context.Id);

            base.OwnedObjectDisposed(ownedObject);
        }
    }
}
