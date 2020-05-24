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
        public void Test_object()
        {
            using (var context = Engine.CreateContext())
            {
                var output = context.Execute("({ a: 1, b: 'bob', c: true, d: new Date(Date.UTC(2020, 04, 18, 14, 32, 16)) })");

                Assert.IsInstanceOf<JsObject>(output);

                dynamic d = output;
                Assert.AreEqual(1, d.a);
                Assert.AreEqual("bob", d.b);
                Assert.AreEqual(true, d.c);
                Assert.AreEqual(new DateTimeOffset(2020, 05, 18, 14, 32, 16, TimeSpan.Zero), d.d);

                context.SetVariable("x", output);
                var output2 = context.GetVariable("x");

                Assert.IsInstanceOf<JsObject>(output2);
                dynamic d2 = output2;
                Assert.AreEqual(1, d2.a);
                Assert.AreEqual("bob", d2.b);
                Assert.AreEqual(true, d2.c);
                Assert.AreEqual(new DateTimeOffset(2020, 05, 18, 14, 32, 16, TimeSpan.Zero), d2.d);
            }
        }
   }
}
