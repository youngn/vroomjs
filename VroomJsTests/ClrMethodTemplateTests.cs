using NUnit.Framework;
using System;
using VroomJs;

namespace VroomJsTests
{
    [TestFixture]
    class ClrMethodTemplateTests
    {
        class Foo
        {
            public string Gamma(string a, int b, int c)
            {
                return a + (b + c).ToString();
            }
            public static string GammaStatic(string a, int b, int c)
            {
                return a + (b + c).ToString();
            }
        }

        class CallbackContext : IHostObjectCallbackContext
        {

        }

        [Test]
        public void Test_Invoke_instance_method()
        {
            var func = new WeakDelegate(new Foo(), nameof(Foo.Gamma));

            var template = new ClrMethodTemplate();
            var result = template.Invoke(new CallbackContext(), func, new object[] { "a", 1, 2 });
            Assert.AreEqual("a3", result);
        }

        [Test]
        public void Test_Invoke_static_method()
        {
            var func = new WeakDelegate(typeof(Foo), nameof(Foo.GammaStatic));

            var template = new ClrMethodTemplate();
            var result = template.Invoke(new CallbackContext(), func, new object[] { "a", 1, 2 });
            Assert.AreEqual("a3", result);
        }

        [Test]
        public void Test_Invoke_too_many_args()
        {
            var func = new WeakDelegate(new Foo(), nameof(Foo.Gamma));

            var template = new ClrMethodTemplate();

            Assert.Throws<MissingMethodException>(
                () => template.Invoke(new CallbackContext(), func, new object[] { "a", 1, 2, 4 }));
        }

        [Test]
        public void Test_Invoke_too_few_args()
        {
            var func = new WeakDelegate(new Foo(), nameof(Foo.Gamma));

            var template = new ClrMethodTemplate();

            Assert.Throws<MissingMethodException>(
                () => template.Invoke(new CallbackContext(), func, new object[] { "a", 1 }));
        }

        [Test]
        public void Test_Invoke_wrong_type_args()
        {
            var func = new WeakDelegate(new Foo(), nameof(Foo.Gamma));

            var template = new ClrMethodTemplate();

            Assert.Throws<MissingMethodException>(
                () => template.Invoke(new CallbackContext(), func, new object[] { 1, 2, 3 }));
        }

        [Test]
        public void Test_Invoke_bad_target()
        {
            var func = new Foo(); // not a func

            var template = new ClrMethodTemplate();

            Assert.Throws<InvalidOperationException>(
                () => template.Invoke(new CallbackContext(), func, new object[] { 1, 2, 3 }));
        }
    }
}
