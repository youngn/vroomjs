using NUnit.Framework;
using System;
using VroomJs;

namespace VroomJsTests
{
    [TestFixture]
    class HostErrorFilterTests
    {
        private class FooException : Exception
        {
            public FooException(string message) : base(message) { }
        }

        [Test]
        public void Test_exception_error_modification()
        {
            var fooEx = new FooException("uh oh");
            using (var engine = new JsEngine())
            {
                // Use filter to modify the error info
                engine.HostErrorFilter = (context, errorInfo) =>
                {
                    Assert.AreSame(fooEx, errorInfo.Exception);

                    errorInfo.Name += errorInfo.Name;
                    errorInfo.Message += " here we go";
                    errorInfo["alpha"] = "bog";
                    errorInfo["beta"] = 10;
                };

                engine.RegisterHostObjectTemplate(new HostObjectTemplate(
                    tryGetProperty: (IHostObjectCallbackContext ctx, object obj, string name, out object value)
                        => { throw fooEx; }
                ));

                using (var context = engine.CreateContext())
                {
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
        }

        [Test]
        public void Test_custom_error_modification()
        {
            using (var engine = new JsEngine())
            {
                // Use filter to modify the error info
                engine.HostErrorFilter = (context, errorInfo) =>
                {
                    Assert.IsNull(errorInfo.Exception);

                    errorInfo.Name += errorInfo.Name;
                    errorInfo.Message += " here we go";
                    errorInfo["alpha"] = "bog";
                    errorInfo["beta"] = 10;
                };

                engine.RegisterHostObjectTemplate(new HostObjectTemplate(
                    tryGetProperty: (IHostObjectCallbackContext ctx, object obj, string name, out object value)
                        => { throw new HostErrorException("uh oh", "MyError"); }
                ));

                using (var context = engine.CreateContext())
                {
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
        }
    }
}
