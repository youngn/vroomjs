using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;
using VroomJs;

namespace VroomJsTests
{
    [TestFixture]
    public class JsEngineTests
    {
        [Test]
        public void Test_create_dispose()
        {
            // Ensure that we can create/dispose multiple engines and contexts

            // in succession
            for(var i = 0; i < 5; i++)
            {
                using (var engine = new JsEngine())
                using (var context = engine.CreateContext())
                {
                }
            }

            // living simultaneously
            var pairs = Enumerable.Range(0, 5).Select(i =>
            {
                var engine = new JsEngine();
                var context = engine.CreateContext();
                return (engine, context);
            }).ToList();

            foreach(var pair in pairs)
            {
                pair.context.Dispose();
                pair.engine.Dispose();
            }

            // in succession on separate threads
            var tasks = Enumerable.Range(0, 5).Select(i => Task.Run(() =>
            {
                using (var engine = new JsEngine())
                using (var context = engine.CreateContext())
                {
                }

            })).ToArray();

            Task.WaitAll(tasks);
        }
    }
}
