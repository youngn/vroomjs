using NUnit.Framework;
using System;
using System.Reflection;
using VroomJs;

namespace VroomJsTests
{
    [TestFixture]
    class ClrDelegateTemplateTests
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
        public void Test_Invoke_closed()
        {
            var foo = new Foo();
            var func = new Func<string, int, int, string>(foo.Gamma);

            var template = new ClrDelegateTemplate();
            var result = template.Invoke(new CallbackContext(), func, new object[] { "a", 1, 2 });
            Assert.AreEqual("a3", result);
        }

        [Test]
        public void Test_Invoke_open()
        {
            var foo = new Foo();
            var func = new Func<string, int, int, string>(Foo.GammaStatic);

            var template = new ClrDelegateTemplate();
            var result = template.Invoke(new CallbackContext(), func, new object[] { "a", 1, 2 });
            Assert.AreEqual("a3", result);
        }

        [Test]
        public void Test_Invoke_too_many_args()
        {
            var foo = new Foo();
            var func = new Func<string, int, int, string>(foo.Gamma);

            var template = new ClrDelegateTemplate();

            Assert.Throws<TargetParameterCountException>(
                () => template.Invoke(new CallbackContext(), func, new object[] { "a", 1, 2, 4 }));
        }

        [Test]
        public void Test_Invoke_too_few_args()
        {
            var foo = new Foo();
            var func = new Func<string, int, int, string>(foo.Gamma);

            var template = new ClrDelegateTemplate();

            Assert.Throws<TargetParameterCountException>(
                () => template.Invoke(new CallbackContext(), func, new object[] { "a", 1 }));
        }

        [Test]
        public void Test_Invoke_wrong_type_args()
        {
            var foo = new Foo();
            var func = new Func<string, int, int, string>(foo.Gamma);

            var template = new ClrDelegateTemplate();

            Assert.Throws<ArgumentException>(
                () => template.Invoke(new CallbackContext(), func, new object[] { 1, 2, 3 }));
        }

        [Test]
        public void Test_Invoke_bad_target()
        {
            var func = new Foo(); // not a func

            var template = new ClrDelegateTemplate();

            Assert.Throws<InvalidOperationException>(
                () => template.Invoke(new CallbackContext(), func, new object[] { 1, 2, 3 }));
        }
    }
}
