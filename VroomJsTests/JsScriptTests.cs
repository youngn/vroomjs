using NUnit.Framework;
using System;
using System.Collections.Generic;
using VroomJs;

namespace VroomJsTests
{
    [TestFixture]
    public class JsScriptTests : TestsBase
    {
        [Test]
        [TestCaseSource(nameof(TestCases_Compile))]
        public object Test_Compile(string code)
        {
            using (var context = Engine.CreateContext())
            {
                using(var script = context.Compile(code))
                {
                    return script.Execute();
                }
            }
        }

        private static IEnumerable<TestCaseData> TestCases_Compile()
        {
            yield return new TestCaseData("").Returns(null);
            yield return new TestCaseData("var a;").Returns(null);
            yield return new TestCaseData("function sqr(x) { return x*x; } sqr(3)").Returns(9);
        }

        [Test]
        public void Test_repeat_execution()
        {
            using (var context = Engine.CreateContext())
            {
                using (var script = context.Compile("++x"))
                {
                    context.SetVariable("x", 0);
                    Assert.AreEqual(1, script.Execute());
                    Assert.AreEqual(2, script.Execute());
                    Assert.AreEqual(3, script.Execute());
                }
            }
        }

        [Test]
        public void Test_cannot_execute_after_Dispose()
        {
            using (var context = Engine.CreateContext())
            {
                JsScript script;
                using (script = context.Compile("++x"))
                {
                    context.SetVariable("x", 0);
                    Assert.AreEqual(1, script.Execute());
                }

                Assert.Throws<ObjectDisposedException>(() => script.Execute());
            }
        }

        [Test]
        public void Test_Dispose()
        {
            using (var context = Engine.CreateContext())
            {
                var script = context.Compile("1");

                script.Dispose();
                // todo: how to verify that native resource was actually disposed?
            }
        }

        [Test]
        public void Test_Dispose_after_context()
        {
            JsScript script;
            using (var context = Engine.CreateContext())
            {
                script = context.Compile("1");
            }

            script.Dispose();
        }

        [Test]
        public void Test_Dispose_after_engine()
        {
            JsScript script;
            using (var engine = new JsEngine())
            using (var context = engine.CreateContext())
            {
                script = context.Compile("1");
            }

            script.Dispose();
        }
    }
}
