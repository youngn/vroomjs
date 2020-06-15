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
                //Assert.AreEqual(3, arr.GetLength());
                foreach (var item in arr)
                    Console.WriteLine(item);
            }
        }
    }
}
