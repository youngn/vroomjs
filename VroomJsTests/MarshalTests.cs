using NUnit.Framework;
using System;
using System.Collections.Generic;
using VroomJs;

namespace VroomJsTests
{
    [TestFixture]
    public class MarshalTests : TestsBase
    {
        [Test]
        [TestCaseSource(nameof(TestCases_roundtrip_primitives))]
        public void Test_roundtrip_primitives(object input)
        {
            using(var context = Engine.CreateContext())
            {
                context.SetVariable("x", input);
                var output = context.GetVariable("x");
                Assert.AreEqual(input, output);
            }
        }

        [Test]
        [TestCaseSource(nameof(TestCases_roundtrip_primitives))]
        public void Test_roundtrip_primitives2(object input)
        {
            using (var context = Engine.CreateContext())
            {
                context.SetVariable("x", input);
                var output = context.Execute("x");
                Assert.AreEqual(input, output);
            }
        }

        private static IEnumerable<TestCaseData> TestCases_roundtrip_primitives()
        {
            // Null = 1,
            yield return new TestCaseData(null);

            // Boolean = 2,
            yield return new TestCaseData(true);
            yield return new TestCaseData(false);

            // Integer = 3,
            yield return new TestCaseData(0);
            yield return new TestCaseData(1);
            yield return new TestCaseData(-1);

            // Number = 4,
            yield return new TestCaseData(0.0);
            yield return new TestCaseData(1.1);
            yield return new TestCaseData(-1.1);

            // String = 5,
            yield return new TestCaseData("");
            yield return new TestCaseData("a");
            yield return new TestCaseData("alpha");
            yield return new TestCaseData("The quick Brown Fox!");

            // Date = 6,
            yield return new TestCaseData(new DateTimeOffset(2020, 05, 18, 14, 32, 16, TimeSpan.Zero));

            // Index = 7,
            // todo: how do we get an index?
        }


        [Test]
        [TestCase("undefined")]
        [TestCase("x = {}; x.a")]
        [TestCase("")]
        [TestCase("var x;")]
        public void Test_undefined(string script)
        {
            using (var context = Engine.CreateContext())
            {
                var output = context.Execute(script);
                Assert.AreEqual(null, output);
            }
        }

        [Test]
        public void Test_array_length()
        {
            using (var context = Engine.CreateContext())
            {
                var output = context.Execute("var x=[1,2,3]; x.length");
                Assert.AreEqual(3, output);
            }
        }

        [Test]
        public void Test_script_error()
        {
            using (var context = Engine.CreateContext())
            {
                try
                {
                    context.Execute("throw new TypeError('Uh oh');");

                    Assert.Fail("Expected the JS error to be thrown as exception on CLR side.");
                }
                catch (JsException e)
                {
                    Console.WriteLine(e);

                    // todo: JS stack trace
                }
            }
        }

        [Test]
        public void Test_JsObject()
        {
            using (var context = Engine.CreateContext())
            {
                var output = context.Execute("({ a: 1, b: 'bob', c: true, d: new Date(Date.UTC(2020, 04, 18, 14, 32, 16)) })");

                Assert.IsInstanceOf<JsObject>(output);

                var obj = (JsObject)output;
                Assert.AreEqual(1, obj["a"]);
                Assert.AreEqual("bob", obj["b"]);
                Assert.AreEqual(true, obj["c"]);
                Assert.AreEqual(new DateTimeOffset(2020, 05, 18, 14, 32, 16, TimeSpan.Zero), obj["d"]);

                context.SetVariable("x", output);
                var output2 = context.GetVariable("x");

                Assert.IsInstanceOf<JsObject>(output2);
                var obj2 = (JsObject)output;
                Assert.AreEqual(1, obj2["a"]);
                Assert.AreEqual("bob", obj2["b"]);
                Assert.AreEqual(true, obj2["c"]);
                Assert.AreEqual(new DateTimeOffset(2020, 05, 18, 14, 32, 16, TimeSpan.Zero), obj2["d"]);
            }
        }

        [Test]
        public void Test_JsArray()
        {
            using (var context = Engine.CreateContext())
            {
                var output = context.Execute("(['a', 1])");

                Assert.IsInstanceOf<JsArray>(output);

                var arr = (JsArray)output;
                Assert.AreEqual("a", arr[0]);
                Assert.AreEqual(1, arr[1]);
                Assert.AreEqual(2, arr["length"]);

                context.SetVariable("x", output);
                var output2 = context.GetVariable("x");

                Assert.IsInstanceOf<JsArray>(output2);
                var arr2 = (JsArray)output;
                Assert.AreEqual("a", arr2[0]);
                Assert.AreEqual(1, arr2[1]);
                Assert.AreEqual(2, arr2["length"]);
            }
        }
    }
}
