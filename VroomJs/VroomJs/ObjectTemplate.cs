using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VroomJs.VroomJs
{
    public class ObjectTemplate
    {
        // Make sure the delegates we pass to the C++ engine won't fly away during a GC.
        private readonly JsCallbacks _callbacks;

        internal ObjectTemplate(int id, JsEngine engine, JsCallbacks callbacks)
        {
            _callbacks = callbacks;
        }
    }
}
