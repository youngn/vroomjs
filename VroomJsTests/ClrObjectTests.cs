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
    public class ClrObjectTests : TestsBase
    {
        class Foo
        {
            public string Alpha { get; set; }

            public int Beta { get; set; }

            public string Gamma(string a, int b, int c)
            {
                return a + (b+c).ToString();
            }

            public int valueOf()
            {
                return 123;
            }

            public string ToString()
            {
                return "f123";
            }
        }


        [Test]
        public void Test_get_set_named_property()
        {
            using (var context = Engine.CreateContext())
            {
                var foo = new Foo() { Alpha = "abc" };
                context.SetVariable("x", foo);
                Assert.IsTrue((bool)context.Execute("x.Alpha === 'abc'"));
                context.Execute("x.Alpha = 'cba'");
                var v = context.Execute("x.Alpha");
                Assert.AreEqual("cba", v);
            }
        }

        [Test]
        public void Test_delete_named_property()
        {
            using (var context = Engine.CreateContext())
            {
                var foo = new Foo() { Alpha = "abc" };
                context.SetVariable("x", foo);
                Assert.IsFalse((bool)context.Execute("delete x.Alpha;"));
                var v = context.Execute("x.Alpha");
                Assert.AreEqual("abc", v);
            }
        }

        [Test]
        public void Test_InvokeMethod_named()
        {
            using (var context = Engine.CreateContext())
            {
                var foo = new Foo();
                context.SetVariable("x", foo);
                var str = (string)context.Execute("x.Gamma('a', 2, 3)");
                Assert.AreEqual("a5", str);
            }
        }

        [Test]
        public void Test_GetPropertyNames()
        {
            using (var context = Engine.CreateContext())
            {
                var foo = new Foo();
                context.SetVariable("x", foo);
                var arr = context.Execute("Object.keys(x)") as JsArray;
                foreach (var item in arr)
                    Console.WriteLine(item);
                Assert.AreEqual(4, arr.GetLength());
            }
        }

        [Test]
        public void Test_valueOf_callback()
        {
            using (var context = Engine.CreateContext())
            {
                var foo = new Foo();
                context.SetVariable("x", foo);
                var result = context.Execute("x.valueOf()");
                Assert.AreEqual(123, result);
            }
        }

        // TODO: right now this effectively delegates to the .NET ToString()
        // method, but maybe we don't want it to? May need to set it up as a
        // special method on the template, with a dedicated callback.
        [Test]
        public void Test_toString_callback()
        {
            using (var context = Engine.CreateContext())
            {
                var foo = new Foo();
                context.SetVariable("x", foo);
                var result = context.Execute("x.toString()");
                Assert.AreEqual("f123", result);
            }
        }
    }
}
