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
    class ClrObjectTemplateTests
    {
        class Foo
        {
            public string Alpha { get; set; }

            public int Beta { get; set; }

            public string Gamma(string a, int b, int c)
            {
                return a + (b + c).ToString();
            }
        }

        class CallbackContext : IHostObjectCallbackContext
        {

        }

        [Test]
        public void Test_TryGetPropertyValue()
        {
            var foo = new Foo() { Alpha = "abc" };

            var template = new ClrObjectTemplate();
            var b = template.TryGetPropertyValue(new CallbackContext(), foo, nameof(Foo.Alpha), out object value);
            Assert.IsTrue(b);
            Assert.AreEqual("abc", value);
        }

        [Test]
        public void Test_TryGetPropertyValue_empty_string()
        {
            var foo = new Foo();

            var template = new ClrObjectTemplate();
            var b = template.TryGetPropertyValue(new CallbackContext(), foo, "", out _);
            Assert.IsFalse(b);
        }

        [Test]
        public void Test_TryGetPropertyValue_method()
        {
            var foo = new Foo();

            var template = new ClrObjectTemplate();
            var b = template.TryGetPropertyValue(new CallbackContext(), foo, nameof(Foo.Gamma), out object value);
            Assert.IsTrue(b);
            Assert.IsInstanceOf<WeakDelegate>(value);
            var del = (WeakDelegate)value;
            Assert.AreEqual(nameof(Foo.Gamma), del.MethodName);
            Assert.AreEqual(foo, del.Target);
            Assert.AreEqual(null, del.Type); // Type is not used for instance methods
        }

        [Test]
        public void Test_TryGetPropertyValue_missing()
        {
            var foo = new Foo();

            var template = new ClrObjectTemplate();
            var b = template.TryGetPropertyValue(new CallbackContext(), foo, "Alpha1", out _);
            Assert.IsFalse(b);
        }

        class A0
        {
            public string toString()
            {
                return "A0";
            }
        }
        class A1
        {
            public string toString { get; }
        }

        /// <summary>
        /// Any member named "toString" is hidden, because we want to force the ToStringHandler
        /// to be invoked.
        /// </summary>
        [Test]
        public void Test_TryGetPropertyValue_toString()
        {
            var template = new ClrObjectTemplate();
            var b0 = template.TryGetPropertyValue(new CallbackContext(), new A0(), nameof(A0.toString), out _);
            Assert.IsFalse(b0);

            var b1 = template.TryGetPropertyValue(new CallbackContext(), new A1(), nameof(A1.toString), out _);
            Assert.IsFalse(b1);
        }

        [Test]
        public void Test_TrySetPropertyValue()
        {
            var foo = new Foo() { Alpha = "abc" };

            var template = new ClrObjectTemplate();
            var b = template.TrySetPropertyValue(new CallbackContext(), foo, "Alpha", "def");
            Assert.IsTrue(b);
            Assert.AreEqual("def", foo.Alpha);
        }

        [Test]
        public void Test_TrySetPropertyValue_method()
        {
            var foo = new Foo();

            var template = new ClrObjectTemplate();
            var b = template.TrySetPropertyValue(new CallbackContext(), foo, nameof(Foo.Gamma), null);
            Assert.IsFalse(b); // cannot modify a method
        }

        [Test]
        public void Test_TrySetPropertyValue_empty_string()
        {
            var foo = new Foo();

            var template = new ClrObjectTemplate();
            var b = template.TrySetPropertyValue(new CallbackContext(), foo, "", "def");
            Assert.IsFalse(b);
        }

        [Test]
        public void Test_TrySetPropertyValue_missing()
        {
            var foo = new Foo();

            var template = new ClrObjectTemplate();
            var b = template.TrySetPropertyValue(new CallbackContext(), foo, "Alpha1", "def");
            Assert.IsFalse(b);
        }

        [Test]
        public void Test_TrySetPropertyValue_toString()
        {
            var template = new ClrObjectTemplate();
            var b0 = template.TrySetPropertyValue(new CallbackContext(), new A0(), nameof(A0.toString), "def");
            Assert.IsFalse(b0);

            var b1 = template.TrySetPropertyValue(new CallbackContext(), new A1(), nameof(A1.toString), "def");
            Assert.IsFalse(b1);
        }

        [Test]
        public void Test_TryDeleteProperty()
        {
            var foo = new Foo() { Alpha = "abc" };

            var template = new ClrObjectTemplate();
            var b = template.TryDeleteProperty(new CallbackContext(), foo, "Alpha", out bool deleted);
            Assert.IsTrue(b);
            Assert.AreEqual(false, deleted);
        }

        [Test]
        public void Test_TryDeleteProperty_missing()
        {
            var foo = new Foo();

            var template = new ClrObjectTemplate();
            var b = template.TryDeleteProperty(new CallbackContext(), foo, "Alpha1", out _);
            Assert.IsFalse(b);
        }

        [Test]
        public void Test_TryDeleteProperty_empty_string()
        {
            var foo = new Foo();

            var template = new ClrObjectTemplate();
            var b = template.TryDeleteProperty(new CallbackContext(), foo, "", out _);
            Assert.IsFalse(b);
        }

        [Test]
        public void Test_TryDeleteProperty_toString()
        {
            var template = new ClrObjectTemplate();
            var b0 = template.TryDeleteProperty(new CallbackContext(), new A0(), nameof(A0.toString), out _);
            Assert.IsFalse(b0);

            var b1 = template.TryDeleteProperty(new CallbackContext(), new A1(), nameof(A1.toString), out _);
            Assert.IsFalse(b1);
        }

        [Test]
        public void Test_EnumerateProperties()
        {
            var foo = new Foo();

            var template = new ClrObjectTemplate();
            var props = template.EnumerateProperties(new CallbackContext(), foo).ToList();

            Assert.AreEqual(3, props.Count);
            Assert.Contains(nameof(Foo.Alpha), props);
            Assert.Contains(nameof(Foo.Beta), props);
            Assert.Contains(nameof(Foo.Gamma), props);
        }

        class B0
        {
            public override string ToString()
            {
                return "B0:1234";
            }
        }

        [Test]
        public void Test_ToString()
        {
            var obj = new B0();

            var template = new ClrObjectTemplate();
            var s = template.ToString(new CallbackContext(), obj);

            Assert.AreEqual("[object B0]", s);
        }

        [Test]
        public void Test_ToString_UseNetToString()
        {
            var obj = new B0();

            var template = new ClrObjectTemplate(useNetToString: true);
            var s = template.ToString(new CallbackContext(), obj);

            Assert.AreEqual("B0:1234", s);
        }
    }
}
