using System.Collections.Generic;
using System.Text;
using VroomJs.Interop;

namespace VroomJs
{
    public class AllocationStats
    {
        private allocationstats _stats;

        public AllocationStats()
        {
            Refresh();
        }

        public void Refresh()
        {
            _stats = NativeApi.js_get_allocation_stats();
        }

        public int EngineCount => _stats.EngineCount;
        public int ContextCount => _stats.ContextCount;
        public int ScriptCount => _stats.ScriptCount;
        public int HostObjectCount => _stats.HostObjectCount;
        public int JsObjectCount => _stats.JsObjectCount;

        public override string ToString()
        {
            var list = new List<string>();
            list.Add($"{EngineCount} engines");
            list.Add($"{ContextCount} contexts");
            list.Add($"{ScriptCount} scripts");
            list.Add($"{HostObjectCount} host object proxies");
            list.Add($"{JsObjectCount} JS object proxies");

            return string.Concat(", ", list);
        }
    }
}
