using NUnit.Framework;
using System;
using VroomJs;

namespace VroomJsTests
{
    [TestFixture]
    public class CleanupTests
    {
        [Test]
        public void Test_explicit_disposal()
        {
            var stats = new AllocationStats();
            Assert.AreEqual(0, stats.EngineCount);
            Assert.AreEqual(0, stats.ContextCount);
            Assert.AreEqual(0, stats.ScriptCount);
            Assert.AreEqual(0, stats.JsObjectCount);
            Assert.AreEqual(0, stats.HostObjectCount);

            var config = new EngineConfiguration();
            config.ClrTemplates.EnableObjects = true;

            using (var engine = new JsEngine(config))
            using (var context = engine.CreateContext())
            using (var script = context.Compile("1 + 1"))
            using (var obj = (JsObject)context.Execute("({})")) // create a JS object
            {
                context.SetVariable("x", new object()); // create a Host object

                stats.Refresh();
                Assert.AreEqual(1, stats.EngineCount);
                Assert.AreEqual(1, stats.ContextCount);
                Assert.AreEqual(1, stats.ScriptCount);
                Assert.AreEqual(1, stats.JsObjectCount);
                Assert.AreEqual(1, stats.HostObjectCount);
            }

            stats.Refresh();
            Assert.AreEqual(0, stats.EngineCount);
            Assert.AreEqual(0, stats.ContextCount);
            Assert.AreEqual(0, stats.ScriptCount);
            Assert.AreEqual(0, stats.JsObjectCount);
            Assert.AreEqual(0, stats.HostObjectCount); // disposed by context
        }

        [Test]
        public void Test_cleanup_via_finalizers()
        {
            var stats = new AllocationStats();
            Assert.AreEqual(0, stats.EngineCount);
            Assert.AreEqual(0, stats.ContextCount);
            Assert.AreEqual(0, stats.ScriptCount);
            Assert.AreEqual(0, stats.JsObjectCount);
            Assert.AreEqual(0, stats.HostObjectCount);

            CreateObjects();

            // Once the local function exits, the objects go out of scope,
            // allowing the finalizers to run

            GC.Collect();
            GC.WaitForPendingFinalizers();

            stats.Refresh();
            Assert.AreEqual(0, stats.EngineCount);
            Assert.AreEqual(0, stats.ContextCount);
            Assert.AreEqual(0, stats.ScriptCount);
            Assert.AreEqual(0, stats.JsObjectCount);
            Assert.AreEqual(0, stats.HostObjectCount);

            void CreateObjects()
            {
                var config = new EngineConfiguration();
                config.ClrTemplates.EnableObjects = true;

                var engine = new JsEngine(config);
                var context = engine.CreateContext();

                var script = context.Compile("1 + 1");
                script.Execute();

                var obj = (JsObject)context.Execute("({})"); // create a JS object
                obj.SetPropertyValue("a", 1);

                //context.SetVariable("x", new object()); // create a Host object

                stats.Refresh();
                Assert.AreEqual(1, stats.EngineCount);
                Assert.AreEqual(1, stats.ContextCount);
                Assert.AreEqual(1, stats.ScriptCount);
                Assert.AreEqual(1, stats.JsObjectCount);
                Assert.AreEqual(0, stats.HostObjectCount);
            }
        }
    }
}
