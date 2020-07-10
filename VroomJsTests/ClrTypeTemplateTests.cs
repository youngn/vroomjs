using NUnit.Framework;
using System.Linq;
using VroomJs;

namespace VroomJsTests
{
    [TestFixture]
    class ClrTypeTemplateTests
    {
        static class Foo
        {
            public static string Alpha { get; set; }

            public static int Beta { get; set; }

            public static string Gamma(string a, int b, int c)
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
            Foo.Alpha = "abc";

            var template = new ClrTypeTemplate();
            var b = template.TryGetPropertyValue(new CallbackContext(), typeof(Foo), nameof(Foo.Alpha), out object value);
            Assert.IsTrue(b);
            Assert.AreEqual("abc", value);
        }

        [Test]
        public void Test_TryGetPropertyValue_empty_string()
        {
            var template = new ClrTypeTemplate();
            var b = template.TryGetPropertyValue(new CallbackContext(), typeof(Foo), "", out _);
            Assert.IsFalse(b);
        }

        [Test]
        public void Test_TryGetPropertyValue_method()
        {
            var template = new ClrTypeTemplate();
            var b = template.TryGetPropertyValue(new CallbackContext(), typeof(Foo), nameof(Foo.Gamma), out object value);
            Assert.IsTrue(b);
            Assert.IsInstanceOf<WeakDelegate>(value);
            var del = (WeakDelegate)value;
            Assert.AreEqual(nameof(Foo.Gamma), del.MethodName);
            Assert.AreEqual(null, del.Target); // Target is not used for static methods
            Assert.AreEqual(typeof(Foo), del.Type);
        }

        [Test]
        public void Test_TryGetPropertyValue_missing()
        {
            var template = new ClrTypeTemplate();
            var b = template.TryGetPropertyValue(new CallbackContext(), typeof(Foo), "Alpha1", out _);
            Assert.IsFalse(b);
        }

        static class A0
        {
            public static string toString()
            {
                return "A0";
            }
        }
        static class A1
        {
            public static string toString { get; }
        }

        /// <summary>
        /// Any member named "toString" is hidden, because we want to force the ToStringHandler
        /// to be invoked.
        /// </summary>
        [Test]
        public void Test_TryGetPropertyValue_toString()
        {
            var template = new ClrTypeTemplate();
            var b0 = template.TryGetPropertyValue(new CallbackContext(), typeof(A0), nameof(A0.toString), out _);
            Assert.IsFalse(b0);

            var b1 = template.TryGetPropertyValue(new CallbackContext(), typeof(A1), nameof(A1.toString), out _);
            Assert.IsFalse(b1);
        }

        [Test]
        public void Test_TrySetPropertyValue()
        {
            Foo.Alpha = "abc";

            var template = new ClrTypeTemplate();
            var b = template.TrySetPropertyValue(new CallbackContext(), typeof(Foo), "Alpha", "def");
            Assert.IsTrue(b);
            Assert.AreEqual("def", Foo.Alpha);
        }

        [Test]
        public void Test_TrySetPropertyValue_method()
        {
            var template = new ClrTypeTemplate();
            var b = template.TrySetPropertyValue(new CallbackContext(), typeof(Foo), nameof(Foo.Gamma), null);
            Assert.IsFalse(b); // cannot modify a method
        }

        [Test]
        public void Test_TrySetPropertyValue_empty_string()
        {
            var template = new ClrTypeTemplate();
            var b = template.TrySetPropertyValue(new CallbackContext(), typeof(Foo), "", "def");
            Assert.IsFalse(b);
        }

        [Test]
        public void Test_TrySetPropertyValue_missing()
        {
            var template = new ClrTypeTemplate();
            var b = template.TrySetPropertyValue(new CallbackContext(), typeof(Foo), "Alpha1", "def");
            Assert.IsFalse(b);
        }

        [Test]
        public void Test_TrySetPropertyValue_toString()
        {
            var template = new ClrTypeTemplate();
            var b0 = template.TrySetPropertyValue(new CallbackContext(), typeof(A0), nameof(A0.toString), "def");
            Assert.IsFalse(b0);

            var b1 = template.TrySetPropertyValue(new CallbackContext(), typeof(A1), nameof(A1.toString), "def");
            Assert.IsFalse(b1);
        }

        [Test]
        public void Test_TryDeleteProperty()
        {
            Foo.Alpha = "abc";

            var template = new ClrTypeTemplate();
            var b = template.TryDeleteProperty(new CallbackContext(), typeof(Foo), "Alpha", out bool deleted);
            Assert.IsTrue(b);
            Assert.AreEqual(false, deleted);
        }

        [Test]
        public void Test_TryDeleteProperty_missing()
        {
            var template = new ClrTypeTemplate();
            var b = template.TryDeleteProperty(new CallbackContext(), typeof(Foo), "Alpha1", out _);
            Assert.IsFalse(b);
        }

        [Test]
        public void Test_TryDeleteProperty_empty_string()
        {
            var template = new ClrTypeTemplate();
            var b = template.TryDeleteProperty(new CallbackContext(), typeof(Foo), "", out _);
            Assert.IsFalse(b);
        }

        [Test]
        public void Test_TryDeleteProperty_toString()
        {
            var template = new ClrTypeTemplate();
            var b0 = template.TryDeleteProperty(new CallbackContext(), typeof(A0), nameof(A0.toString), out _);
            Assert.IsFalse(b0);

            var b1 = template.TryDeleteProperty(new CallbackContext(), typeof(A1), nameof(A1.toString), out _);
            Assert.IsFalse(b1);
        }

        [Test]
        public void Test_EnumerateProperties()
        {
            var template = new ClrTypeTemplate();
            var props = template.EnumerateProperties(new CallbackContext(), typeof(Foo)).ToList();

            Assert.AreEqual(3, props.Count);
            Assert.Contains(nameof(Foo.Alpha), props);
            Assert.Contains(nameof(Foo.Beta), props);
            Assert.Contains(nameof(Foo.Gamma), props);
        }

        static class B0
        {
        }

        [Test]
        public void Test_ToString()
        {
            var template = new ClrTypeTemplate();
            var s = template.ToString(new CallbackContext(), typeof(B0));

            Assert.AreEqual("[object B0]", s);
        }

        class C0
        {
            public C0(int a)
            {
                A = a;
            }

            public int A { get; }
        }


        [Test]
        public void Test_InvokeHandler()
        {
            var template0 = new ClrTypeTemplate(allowInvokeConstructor: false);
            Assert.IsNull(template0.InvokeHandler);

            var template = new ClrTypeTemplate(allowInvokeConstructor: true);
            Assert.IsNotNull(template.InvokeHandler);
            var obj = template.InvokeHandler(new CallbackContext(), typeof(C0), new object[] { 2 });

            Assert.IsInstanceOf<C0>(obj);
            var c0 = (C0)obj;
            Assert.AreEqual(2, c0.A);
        }
    }
}
