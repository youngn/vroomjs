using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VroomJs;

namespace VroomJsTests
{
    [TestFixture]
    public class JsContextTests : TestsBase
    {
        [Test]
        [TestCaseSource(nameof(TestCases_Execute))]
        public object Test_Execute(string script)
        {
            using (var context = Engine.CreateContext())
            {
                return context.Execute(script);
            }
        }

        private static IEnumerable<TestCaseData> TestCases_Execute()
        {
            yield return new TestCaseData("").Returns(null);
            yield return new TestCaseData("var a;").Returns(null);
            yield return new TestCaseData("1").Returns(1);
            yield return new TestCaseData("'1'").Returns("1");
        }

        [Test]
        public void Test_Execute_runtime_error()
        {
            using (var context = Engine.CreateContext())
            {
                Assert.Throws<JsException>(
                    () => context.Execute("throw new Error('ouch')"));
            }
        }

        [Test]
        public void Test_Execute_syntax_error()
        {
            using (var context = Engine.CreateContext())
            {
                Assert.Throws<JsSyntaxException>(
                    () => context.Execute("({{"));
            }
        }

        [Test]
        public void Test_Execute_null_string()
        {
            using (var context = Engine.CreateContext())
            {
                Assert.Throws<ArgumentNullException>(
                    () => context.Execute((string)null));
            }
        }

        [Test]
        [TestCaseSource(nameof(TestCases_SetVariable))]
        public object Test_SetVariable(string name, object value)
        {
            using (var context = Engine.CreateContext())
            {
                context.SetVariable(name, value);
                return context.GetVariable(name);
            }
        }

        private static IEnumerable<TestCaseData> TestCases_SetVariable()
        {
            yield return new TestCaseData("a", 1).Returns(1);
            yield return new TestCaseData("a", "bob").Returns("bob");
            yield return new TestCaseData("a", false).Returns(false);
            yield return new TestCaseData("a", null).Returns(null);
            yield return new TestCaseData("a", "").Returns("");
            yield return new TestCaseData("a", 0).Returns(0);

            // Apparently setting the empty string is legal! (this works in Chrome too)
            yield return new TestCaseData("", 1).Returns(1);
        }

        [Test]
        public void Test_SetVariable_null_key()
        {
            using (var context = Engine.CreateContext())
            {
                Assert.Throws<ArgumentNullException>(
                    () => context.SetVariable(null, 1));
            }
        }

        [Test]
        public void Test_GetVariable_null_key()
        {
            using (var context = Engine.CreateContext())
            {
                Assert.Throws<ArgumentNullException>(
                    () => context.GetVariable(null));
            }
        }

        [Test]
        public void Test_GetVariable_that_does_not_exist()
        {
            using (var context = Engine.CreateContext())
            {
                Assert.AreEqual(null, context.GetVariable("alpha"));
            }
        }
    }
}
