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
            using (var context = Engine.CreateContext())
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
            const string script =
                "function alpha() {\n" +
                "    throw new TypeError('Uh oh');\n" +
                "}\n" +
                "function beta() {\n" +
                "    alpha();\n" +
                "}\n" +
                "function gamma() {\n" +
                "    beta();\n" +
                "}\n" +
                "\n" +
                "gamma()";

            const string stackStr = "TypeError: Uh oh\n" +
                "    at alpha (<Unnamed Script>:2:11)\n" + 
                "    at beta (<Unnamed Script>:5:5)\n" + 
                "    at gamma (<Unnamed Script>:8:5)\n" +
                "    at <Unnamed Script>:11:1";

            using (var context = Engine.CreateContext())
            {
                try
                {
                    context.Execute(script);

                    Assert.Fail("Expected the JS error to be thrown as exception on CLR side.");
                }
                catch (JsException e)
                {
                    //Console.WriteLine(e);

                    Assert.AreEqual(2, e.Line);
                    Assert.AreEqual(4, e.Column);
                    Assert.AreEqual("TypeError", e.ErrorName);
                    Assert.AreEqual("TypeError: Uh oh", e.ErrorText);
                    Assert.AreEqual("Uncaught TypeError: Uh oh", e.Description);
                    Assert.AreEqual(stackStr, e.ErrorStackString);
                    Assert.AreEqual(stackStr, e.Message); // designed to be identical to ErrorStackString

                    Assert.IsNotNull(e.ErrorStackTrace);

                    var error = e.Error as JsObject;
                    Assert.IsNotNull(error);
                    Assert.AreEqual("Uh oh", error["message"]);
                    Assert.AreEqual("TypeError", error["name"]);
                }
            }
        }

        [Test]
        public void Test_script_error_throws_string()
        {
            const string script =
                "function alpha() {\n" +
                "    throw 'Uh oh';\n" +
                "}\n" +
                "function beta() {\n" +
                "    alpha();\n" +
                "}\n" +
                "function gamma() {\n" +
                "    beta();\n" +
                "}\n" +
                "\n" +
                "gamma()";

            const string stackStr = "Uh oh\n" +
                "    at alpha (<Unnamed Script>:2:5)\n" +
                "    at beta (<Unnamed Script>:5:5)\n" +
                "    at gamma (<Unnamed Script>:8:5)\n" +
                "    at <Unnamed Script>:11:1";

            using (var context = Engine.CreateContext())
            {
                try
                {
                    context.Execute(script);

                    Assert.Fail("Expected the JS error to be thrown as exception on CLR side.");
                }
                catch (JsException e)
                {
                    //Console.WriteLine(e);

                    Assert.AreEqual(2, e.Line);
                    Assert.AreEqual(4, e.Column);
                    Assert.AreEqual(null, e.ErrorName); // no .name property available
                    Assert.AreEqual("Uh oh", e.ErrorText);
                    Assert.AreEqual("Uncaught Uh oh", e.Description);
                    Assert.AreEqual(null, e.ErrorStackString); // no .stack property available
                    Assert.AreEqual(stackStr, e.Message);

                    Assert.IsNotNull(e.ErrorStackTrace);

                    var error = e.Error as string;
                    Assert.IsNotNull(error);
                    Assert.AreEqual("Uh oh", error);
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
                var obj2 = (JsObject)output2;

                // todo: make this assertion hold
                //Assert.AreSame(obj, obj2);

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
                var arr2 = (JsArray)output2;

                // todo: make this assertion hold
                //Assert.AreSame(arr, arr2);

                Assert.AreEqual("a", arr2[0]);
                Assert.AreEqual(1, arr2[1]);
                Assert.AreEqual(2, arr2["length"]);
            }
        }

        [Test]
        public void Test_JsFunction()
        {
            // Same idea as Test_JsObject above
            Assert.Fail("TODO");
        }

        class Foo
        {

        }

        [Test]
        public void Test_clr_object()
        {
            using (var context = Engine.CreateContext())
            {
                var foo = new Foo();
                context.SetVariable("x", foo);

                // Ensure round-trippable
                var fooReturned = context.GetVariable("x");
                Assert.AreSame(foo, fooReturned);

                // Ensure object identity is maintained on script side
                context.SetVariable("y", foo);
                Assert.IsTrue((bool)context.Execute("y === x"));

                var foo2 = new Foo();
                context.SetVariable("y", foo2);
                var foo2Returned = context.GetVariable("y");
                Assert.AreSame(foo2, foo2Returned);
                Assert.AreNotSame(foo, foo2Returned);

                Assert.IsFalse((bool)context.Execute("y === x"));
            }
        }
    }
}
