using NUnit.Framework;
using System;
using System.Collections.Generic;
using VroomJs;

namespace VroomJsTests
{
    [TestFixture]
    class HostErrorFilterTests : TestsBase
    {
        private class FooException : Exception
        {
            public FooException(string message) : base(message) { }
        }

        [Test]
        public void Test_exception_error_modification()
        {
            var fooEx = new FooException("uh oh");

            using (var context = Engine.CreateContext())
            {
                // Use filter to modify the error info
                context.HostErrorFilter = (ctx, errorInfo) =>
                {
                    Assert.AreSame(fooEx, errorInfo.Exception);

                    errorInfo.Name += errorInfo.Name;
                    errorInfo.Message += " here we go";
                    errorInfo["alpha"] = "bog";
                    errorInfo["beta"] = 10;
                    return true;
                };

                context.RegisterHostObjectTemplate(new HostObjectTemplate(
                    tryGetProperty: (IHostObjectCallbackContext ctx, object obj, string name, out object value)
                        => { throw fooEx; }
                ));

                var x = new object();
                context.SetVariable("x", x);

                var result = context.Execute("try { x.bar; } catch(e) { [e.name, e.message, e.alpha, e.beta]; }") as JsObject;
                Assert.IsNotNull(result);
                Assert.AreEqual("ErrorError", result[0]);
                Assert.AreEqual("uh oh here we go", result[1]);
                Assert.AreEqual("bog", result[2]);
                Assert.AreEqual(10, result[3]);
            }
        }

        [Test]
        [TestCaseSource(nameof(TestCases_error_suppression))]
        public void Test_exception_error_suppression(string script)
        {
            var fooEx = new FooException("uh oh");

            using (var context = Engine.CreateContext())
            {
                // Use filter to prevent host error from being caught by script
                context.HostErrorFilter = (ctx, errorInfo) =>
                {
                    Assert.AreSame(fooEx, errorInfo.Exception);

                    return false;
                };

                context.RegisterHostObjectTemplate(new HostObjectTemplate(
                    tryGetProperty: (IHostObjectCallbackContext ctx, object obj, string name, out object value)
                        => { throw fooEx; }
                ));

                var x = new object();
                context.SetVariable("x", x);

                var ex = Assert.Throws<FooException>(() =>
                {
                    context.Execute(script);
                });

                Assert.AreSame(fooEx, ex);
            }
        }

        [Test]
        public void Test_custom_error_modification()
        {
            using (var context = Engine.CreateContext())
            {
                // Use filter to modify the error info
                context.HostErrorFilter = (ctx, errorInfo) =>
                {
                    Assert.IsNull(errorInfo.Exception);

                    errorInfo.Name += errorInfo.Name;
                    errorInfo.Message += " here we go";
                    errorInfo["alpha"] = "bog";
                    errorInfo["beta"] = 10;
                    return true;
                };

                context.RegisterHostObjectTemplate(new HostObjectTemplate(
                    tryGetProperty: (IHostObjectCallbackContext ctx, object obj, string name, out object value)
                        => { throw new HostErrorException("uh oh", "MyError"); }
                ));

                var x = new object();
                context.SetVariable("x", x);

                var result = context.Execute("try { x.bar; } catch(e) { [e.name, e.message, e.alpha, e.beta]; }") as JsObject;
                Assert.IsNotNull(result);
                Assert.AreEqual("MyErrorMyError", result[0]);
                Assert.AreEqual("uh oh here we go", result[1]);
                Assert.AreEqual("bog", result[2]);
                Assert.AreEqual(10, result[3]);
            }
        }

        [Test]
        [TestCaseSource(nameof(TestCases_error_suppression))]
        public void Test_custom_error_suppression(string script)
        {
            using (var context = Engine.CreateContext())
            {
                // Use filter to prevent host error from being caught by script
                context.HostErrorFilter = (ctx, errInfo) =>
                {
                    Assert.IsNull(errInfo.Exception);

                    return false;
                };

                context.RegisterHostObjectTemplate(new HostObjectTemplate(
                    tryGetProperty: (IHostObjectCallbackContext ctx, object obj, string name, out object value)
                        => { throw new HostErrorException("uh oh", "MyError"); }
                ));

                var x = new object();
                context.SetVariable("x", x);

                var ex = Assert.Throws<HostErrorException>(() =>
                {
                    context.Execute(script);
                });

                var errorInfo = ex.ErrorInfo;
                Assert.IsNotNull(errorInfo);
                Assert.AreEqual("MyError", errorInfo.Name);
                Assert.AreEqual("uh oh", errorInfo.Message);
                Assert.AreEqual(null, errorInfo.Exception);
            }
        }

        private static IEnumerable<string> TestCases_error_suppression()
        {
            yield return
                "try { " +
                "   for(var i = 0; i < 100; i++) {" +
                "       x.bar;" +
                "   }" +
                "} catch(e) {" +
                "}";

            yield return
                "try { " +
                "   var y = x.bar; y++;" +
                "} catch(e) {" +
                "}";
        }
    }
}
