using NUnit.Framework;
using VroomJs;

namespace VroomJsTests
{
    [TestFixture]
    public class JsFunctionTests : TestsBase
    {
        [Test]
        [TestCase(null)]
        [TestCase(1)]
        [TestCase("bob")]
        public void Test_Invoke_simple(object x)
        {
            using (var context = Engine.CreateContext())
            {
                var func = (JsFunction)context.Execute("(function(x) { return x; } )");
                var result = func.Invoke(null, x);
                Assert.AreEqual(x, result);
            }
        }

        [Test]
        public void Test_Invoke_no_args()
        {
            using (var context = Engine.CreateContext())
            {
                var func = (JsFunction)context.Execute("(function(x) { return x; } )");
                var result = func.Invoke(null);
                Assert.AreEqual(null, result);
            }
        }

        [Test]
        [TestCase(null, null, 0)]
        [TestCase(1, 2, 3)]
        [TestCase("bob", "foo", "bobfoo")]
        public void Test_Invoke_multiple_args(object x, object y, object expectedResult)
        {
            using (var context = Engine.CreateContext())
            {
                var func = (JsFunction)context.Execute("(function(x, y) { return x + y; } )");
                var result = func.Invoke(null, x, y);
                Assert.AreEqual(expectedResult, result);
            }
        }

        [Test]
        public void Test_Invoke_with_receiver()
        {
            using (var context = Engine.CreateContext())
            {
                var rec = context.Execute("({ a: 1, b: 2 })");
                var func = (JsFunction)context.Execute("(function() { return this.a + this.b; } )");
                var result = func.Invoke(rec);
                Assert.AreEqual(3, result);
            }
        }

        [Test]
        public void Test_Invoke_with_receiver_not_supplied()
        {
            using (var context = Engine.CreateContext())
            {
                context.SetVariable("x", "thismagicmoment123");
                var func = (JsFunction)context.Execute("(function() { return this; } )");
                var result = func.Invoke(null);

                // the global object was used as the receiver
                Assert.IsInstanceOf<JsObject>(result);
                var obj = (JsObject)result;
                Assert.AreEqual("thismagicmoment123", obj["x"]);
            }
        }
    }
}
