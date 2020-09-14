using System.Collections.Generic;
using System.Reflection;
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

        // todo: is this needed?
        private readonly HashSet<JsContext> _aliveContexts = new HashSet<JsContext>();

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
            _aliveContexts.Add(ctx);

            return ctx;
        }

        public void DumpHeapStats()
        {
            NativeApi.jsengine_dump_heap_stats(Handle);
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
            _aliveContexts.Remove(context);

            base.OwnedObjectDisposed(ownedObject);
        }
    }
}
