using NUnit.Framework;
using System;
using System.Linq;
using VroomJs;

namespace VroomJsTests
{
    [TestFixture]
    class HostObjectTemplateTests : TestsBase
    {
        private class FooException : Exception
        {
            public FooException(string message) : base(message) { }
        }

        private const string DefaultErrorName = "Error";
        private const string CustomErrorName = "CustomError";

        [Test]
        public void Test_named_property()
        {
            object propValue = null;
            using (var context = Engine.CreateContext())
            {
                // Property handlers are installed and return true (handled)
                context.RegisterHostObjectTemplate(new HostObjectTemplate(
                    tryGetProperty: (IHostObjectCallbackContext ctx, object obj, string name, out object value) => { value = propValue; return true; },
                    trySetProperty: (IHostObjectCallbackContext ctx, object obj, string name, object value) => { propValue = value; return true; },
                    tryDeleteProperty: (IHostObjectCallbackContext ctx, object obj, string name, out bool deleted) => { propValue = null; deleted = true; return true; }
                ));

                var x = new object();
                context.SetVariable("x", x);

                Assert.AreEqual(null, context.Execute("x.bar"));
                Assert.AreEqual(24, context.Execute("x.bar = 24"));
                Assert.AreEqual(24, propValue);
                Assert.AreEqual(24, context.Execute("x.bar"));

                Assert.AreEqual(true, context.Execute("delete x.bar"));
                Assert.AreEqual(null, context.Execute("x.bar"));
            }
        }

        [Test]
        public void Test_named_property_no_handler()
        {
            using (var context = Engine.CreateContext())
            {
                // No get/set handler is installed on the template
                context.RegisterHostObjectTemplate(new HostObjectTemplate());

                var x = new object();
                context.SetVariable("x", x);

                // Even though we don't have any handlers registered,
                // we can still get/set arbitrary properties on the JS proxy object.
                Assert.AreEqual(null, context.Execute("x.bar"));
                Assert.AreEqual(24, context.Execute("x.bar = 24"));
                Assert.AreEqual(24, context.Execute("x.bar"));

                var props = context.Execute("Object.keys(x)") as JsArray;
                Assert.IsNotNull(props);
                Assert.AreEqual(1, props.GetLength());
                Assert.AreEqual("bar", props[0]);

                Assert.AreEqual(true, context.Execute("delete x.bar"));
                Assert.AreEqual(null, context.Execute("x.bar"));
            }
        }

        [Test]
        public void Test_named_property_not_handled()
        {
            using (var context = Engine.CreateContext())
            {
                // Property handlers are installed, but return false (not handled)
                context.RegisterHostObjectTemplate(new HostObjectTemplate(
                    tryGetProperty: (IHostObjectCallbackContext ctx, object obj, string name, out object value) => { value = null; return false; },
                    trySetProperty: (IHostObjectCallbackContext ctx, object obj, string name, object value) => { return false; },
                    tryDeleteProperty: (IHostObjectCallbackContext ctx, object obj, string name, out bool deleted) => { deleted = false; return false; }
                ));

                var x = new object();
                context.SetVariable("x", x);

                // Given that the handlers return false, all requests
                // fall through to the JS proxy object.
                Assert.AreEqual(null, context.Execute("x.bar"));
                Assert.AreEqual(24, context.Execute("x.bar = 24"));
                Assert.AreEqual(24, context.Execute("x.bar"));

                var props = context.Execute("Object.keys(x)") as JsArray;
                Assert.IsNotNull(props);
                Assert.AreEqual(1, props.GetLength());
                Assert.AreEqual("bar", props[0]);

                Assert.AreEqual(true, context.Execute("delete x.bar"));
                Assert.AreEqual(null, context.Execute("x.bar"));
            }
        }

        [Test]
        public void Test_get_property_throws_exception()
        {
            var fooEx = new FooException("uh oh");

            using (var context = Engine.CreateContext())
            {
                // Property handlers are installed and return true (handled)
                context.RegisterHostObjectTemplate(new HostObjectTemplate(
                    tryGetProperty: (IHostObjectCallbackContext ctx, object obj, string name, out object value)
                        => { throw fooEx; }
                ));

                var x = new object();
                context.SetVariable("x", x);

                var ex = Assert.Throws<JsException>(() =>
                {
                    context.Execute("x.bar");
                });

                Assert.AreEqual(DefaultErrorName, ex.ErrorInfo.Name);
                var errorObj = (JsObject)ex.ErrorInfo.Error;
                Assert.AreEqual("uh oh", errorObj["message"]);
                Assert.AreSame(fooEx, ex.InnerException);
                Assert.AreSame(fooEx, ex.ErrorInfo.ClrException);
            }
        }

        [Test]
        public void Test_get_property_throws_custom_error()
        {
            using (var context = Engine.CreateContext())
            {
                // Property handlers are installed and return true (handled)
                context.RegisterHostObjectTemplate(new HostObjectTemplate(
                    tryGetProperty: (IHostObjectCallbackContext ctx, object obj, string name, out object value)
                        => { throw new HostErrorException("uh oh", CustomErrorName); }
                ));

                var x = new object();
                context.SetVariable("x", x);

                var ex = Assert.Throws<JsException>(() =>
                {
                    context.Execute("x.bar");
                });

                Assert.AreEqual(CustomErrorName, ex.ErrorInfo.Name);
                var errorObj = (JsObject)ex.ErrorInfo.Error;
                Assert.AreEqual("uh oh", errorObj["message"]);
                Assert.IsNull(ex.InnerException);
                Assert.IsNull(ex.ErrorInfo.ClrException);
            }
        }

        [Test]
        public void Test_set_property_throws_exception()
        {
            var fooEx = new FooException("uh oh");

            using (var context = Engine.CreateContext())
            {
                // Property handlers are installed and return true (handled)
                context.RegisterHostObjectTemplate(new HostObjectTemplate(
                    trySetProperty: (IHostObjectCallbackContext ctx, object obj, string name, object value)
                        => { throw fooEx; }
                ));

                var x = new object();
                context.SetVariable("x", x);

                var ex = Assert.Throws<JsException>(() =>
                {
                    context.Execute("x.bar = 1;");
                });

                Assert.AreEqual(DefaultErrorName, ex.ErrorInfo.Name);
                var errorObj = (JsObject)ex.ErrorInfo.Error;
                Assert.AreEqual("uh oh", errorObj["message"]);
                Assert.AreSame(fooEx, ex.InnerException);
                Assert.AreSame(fooEx, ex.ErrorInfo.ClrException);
            }
        }

        [Test]
        public void Test_set_property_throws_custom_error()
        {
            using (var context = Engine.CreateContext())
            {
                // Property handlers are installed and return true (handled)
                context.RegisterHostObjectTemplate(new HostObjectTemplate(
                    trySetProperty: (IHostObjectCallbackContext ctx, object obj, string name, object value)
                        => { throw new HostErrorException("uh oh", CustomErrorName); }
                ));

                var x = new object();
                context.SetVariable("x", x);

                var ex = Assert.Throws<JsException>(() =>
                {
                    context.Execute("x.bar = 1;");
                });

                Assert.AreEqual(CustomErrorName, ex.ErrorInfo.Name);
                var errorObj = (JsObject)ex.ErrorInfo.Error;
                Assert.AreEqual("uh oh", errorObj["message"]);
                Assert.IsNull(ex.InnerException);
                Assert.IsNull(ex.ErrorInfo.ClrException);
            }
        }

        [Test]
        public void Test_delete_property_throws_exception()
        {
            var fooEx = new FooException("uh oh");

            using (var context = Engine.CreateContext())
            {
                // Property handlers are installed and return true (handled)
                context.RegisterHostObjectTemplate(new HostObjectTemplate(
                    tryDeleteProperty: (IHostObjectCallbackContext ctx, object obj, string name, out bool deleted)
                        => { throw fooEx; }
                ));

                var x = new object();
                context.SetVariable("x", x);

                var ex = Assert.Throws<JsException>(() =>
                {
                    context.Execute("delete x.bar;");
                });

                Assert.AreEqual(DefaultErrorName, ex.ErrorInfo.Name);
                var errorObj = (JsObject)ex.ErrorInfo.Error;
                Assert.AreEqual("uh oh", errorObj["message"]);
                Assert.AreSame(fooEx, ex.InnerException);
                Assert.AreSame(fooEx, ex.ErrorInfo.ClrException);
            }
        }

        [Test]
        public void Test_delete_property_throws_custom_error()
        {
            using (var context = Engine.CreateContext())
            {
                // Property handlers are installed and return true (handled)
                context.RegisterHostObjectTemplate(new HostObjectTemplate(
                    tryDeleteProperty: (IHostObjectCallbackContext ctx, object obj, string name, out bool deleted)
                        => { throw new HostErrorException("uh oh", CustomErrorName); }
                ));

                var x = new object();
                context.SetVariable("x", x);

                var ex = Assert.Throws<JsException>(() =>
                {
                    context.Execute("delete x.bar;");
                });

                Assert.AreEqual(CustomErrorName, ex.ErrorInfo.Name);
                var errorObj = (JsObject)ex.ErrorInfo.Error;
                Assert.AreEqual("uh oh", errorObj["message"]);
                Assert.IsNull(ex.InnerException);
                Assert.IsNull(ex.ErrorInfo.ClrException);
            }
        }

        [Test]
        public void Test_enumerate_properties()
        {
            using (var context = Engine.CreateContext())
            {
                context.RegisterHostObjectTemplate(new HostObjectTemplate(
                    enumerateProperties: (IHostObjectCallbackContext ctx, object obj) => { return new string[] { "alpha", "beta", "gamma" }; }
                ));

                var x = new object();
                context.SetVariable("x", x);

                var props = context.Execute("Object.keys(x)") as JsArray;
                Assert.IsNotNull(props);
                Assert.AreEqual(3, props.GetLength());
                Assert.AreEqual("alpha", props[0]);
                Assert.AreEqual("beta", props[1]);
                Assert.AreEqual("gamma", props[2]);

                // additional properties added to the JS proxy object are also enumerated
                context.Execute("x.bar = null");
                context.Execute("x.zee = 0");
                props = context.Execute("Object.keys(x)") as JsArray;
                Assert.IsNotNull(props);
                Assert.AreEqual(5, props.GetLength());
                Assert.AreEqual("bar", props[0]);
                Assert.AreEqual("zee", props[1]);
                Assert.AreEqual("alpha", props[2]);
                Assert.AreEqual("beta", props[3]);
                Assert.AreEqual("gamma", props[4]);
            }
        }

        [Test]
        public void Test_enumerate_properties_throws_exception()
        {
            var fooEx = new FooException("uh oh");

            using (var context = Engine.CreateContext())
            {
                context.RegisterHostObjectTemplate(new HostObjectTemplate(
                    enumerateProperties: (IHostObjectCallbackContext ctx, object obj) => { throw fooEx; }
                ));

                var x = new object();
                context.SetVariable("x", x);

                var ex = Assert.Throws<JsException>(() =>
                {
                    context.Execute("Object.keys(x);");
                });

                Assert.AreEqual(DefaultErrorName, ex.ErrorInfo.Name);
                var errorObj = (JsObject)ex.ErrorInfo.Error;
                Assert.AreEqual("uh oh", errorObj["message"]);
                Assert.AreSame(fooEx, ex.InnerException);
                Assert.AreSame(fooEx, ex.ErrorInfo.ClrException);
            }
        }

        [Test]
        public void Test_enumerate_properties_throws_custom_error()
        {
            using (var context = Engine.CreateContext())
            {
                context.RegisterHostObjectTemplate(new HostObjectTemplate(
                    enumerateProperties: (IHostObjectCallbackContext ctx, object obj)
                        => { throw new HostErrorException("uh oh", CustomErrorName); }
                ));

                var x = new object();
                context.SetVariable("x", x);

                var ex = Assert.Throws<JsException>(() =>
                {
                    context.Execute("Object.keys(x);");
                });

                Assert.AreEqual(CustomErrorName, ex.ErrorInfo.Name);
                var errorObj = (JsObject)ex.ErrorInfo.Error;
                Assert.AreEqual("uh oh", errorObj["message"]);
                Assert.IsNull(ex.InnerException);
                Assert.IsNull(ex.ErrorInfo.ClrException);
            }
        }

        [Test]
        public void Test_invoke()
        {
            using (var context = Engine.CreateContext())
            {
                context.RegisterHostObjectTemplate(new HostObjectTemplate(
                    invoke: (IHostObjectCallbackContext ctx, object obj, object[] args) => { return args.Sum(x => (int)x); }
                ));

                var f = new object();
                context.SetVariable("f", f);

                Assert.IsTrue((bool)context.Execute("f() === 0"));
                Assert.IsTrue((bool)context.Execute("f(1) === 1"));
                Assert.IsTrue((bool)context.Execute("f(1, 2) === 3"));
                Assert.IsTrue((bool)context.Execute("f(1, 2, 3) === 6"));
            }
        }

        [Test]
        public void Test_invoke_returns_undefined()
        {
            using (var context = Engine.CreateContext())
            {
                context.RegisterHostObjectTemplate(new HostObjectTemplate(
                    invoke: (IHostObjectCallbackContext ctx, object obj, object[] args) => { return JsUndefined.Value; }
                ));

                var f = new object();
                context.SetVariable("f", f);

                Assert.IsTrue((bool)context.Execute("f() === undefined"));
                Assert.IsFalse((bool)context.Execute("f() === null"));
            }
        }

        [Test]
        public void Test_invoke_returns_null()
        {
            using (var context = Engine.CreateContext())
            {
                context.RegisterHostObjectTemplate(new HostObjectTemplate(
                    invoke: (IHostObjectCallbackContext ctx, object obj, object[] args) => { return null; }
                ));

                var f = new object();
                context.SetVariable("f", f);

                Assert.IsTrue((bool)context.Execute("f() === null"));
                Assert.IsFalse((bool)context.Execute("f() === undefined"));
            }
        }

        [Test]
        public void Test_invoke_throws_exception()
        {
            var fooEx = new FooException("uh oh");
            using (var context = Engine.CreateContext())
            {
                context.RegisterHostObjectTemplate(new HostObjectTemplate(
                    invoke: (IHostObjectCallbackContext ctx, object obj, object[] args) => { throw fooEx; }
                ));

                var f = new object();
                context.SetVariable("f", f);

                var ex = Assert.Throws<JsException>(() =>
                {
                    context.Execute("f()");
                });

                Assert.AreEqual(DefaultErrorName, ex.ErrorInfo.Name);
                var errorObj = (JsObject)ex.ErrorInfo.Error;
                Assert.AreEqual("uh oh", errorObj["message"]);
                Assert.AreSame(fooEx, ex.InnerException);
                Assert.AreSame(fooEx, ex.ErrorInfo.ClrException);
            }
        }

        [Test]
        public void Test_invoke_throws_custom_error()
        {
            using (var context = Engine.CreateContext())
            {
                context.RegisterHostObjectTemplate(new HostObjectTemplate(
                    invoke: (IHostObjectCallbackContext ctx, object obj, object[] args)
                        => { throw new HostErrorException("uh oh", CustomErrorName); }
                ));

                var f = new object();
                context.SetVariable("f", f);

                var ex = Assert.Throws<JsException>(() =>
                {
                    context.Execute("f()");
                });

                Assert.AreEqual(CustomErrorName, ex.ErrorInfo.Name);
                var errorObj = (JsObject)ex.ErrorInfo.Error;
                Assert.AreEqual("uh oh", errorObj["message"]);
                Assert.IsNull(ex.InnerException);
                Assert.IsNull(ex.ErrorInfo.ClrException);
            }
        }

        [Test]
        public void Test_valueOf()
        {
            using (var context = Engine.CreateContext())
            {
                context.RegisterHostObjectTemplate(new HostObjectTemplate(
                    valueOf: (IHostObjectCallbackContext ctx, object obj) => { return 1; }
                ));

                var x = new object();
                context.SetVariable("x", x);
                Assert.AreEqual(1, context.Execute("x.valueOf()"));
            }
        }

        [Test]
        public void Test_valueOf_no_handler()
        {
            using (var context = Engine.CreateContext())
            {
                // No valueOf handler installed on template
                context.RegisterHostObjectTemplate(new HostObjectTemplate());

                var x = new object();
                context.SetVariable("x", x);
                Assert.AreEqual(x, context.Execute("x.valueOf()")); // default V8 implementation
            }
        }

        [Test]
        public void Test_valueOf_throws_exception()
        {
            var fooEx = new FooException("uh oh");
            using (var context = Engine.CreateContext())
            {
                context.RegisterHostObjectTemplate(new HostObjectTemplate(
                    valueOf: (IHostObjectCallbackContext ctx, object obj) => { throw fooEx; }
                ));

                var x = new object();
                context.SetVariable("x", x);

                var ex = Assert.Throws<JsException>(() =>
                {
                    context.Execute("x.valueOf()");
                });

                Assert.AreEqual(DefaultErrorName, ex.ErrorInfo.Name);
                var errorObj = (JsObject)ex.ErrorInfo.Error;
                Assert.AreEqual("uh oh", errorObj["message"]);
                Assert.AreSame(fooEx, ex.InnerException);
                Assert.AreSame(fooEx, ex.ErrorInfo.ClrException);
            }
        }

        [Test]
        public void Test_valueOf_throws_custom_error()
        {
            using (var context = Engine.CreateContext())
            {
                context.RegisterHostObjectTemplate(new HostObjectTemplate(
                    valueOf: (IHostObjectCallbackContext ctx, object obj)
                        => { throw new HostErrorException("uh oh", CustomErrorName); }
                ));

                var x = new object();
                context.SetVariable("x", x);

                var ex = Assert.Throws<JsException>(() =>
                {
                    context.Execute("x.valueOf()");
                });

                Assert.AreEqual(CustomErrorName, ex.ErrorInfo.Name);
                var errorObj = (JsObject)ex.ErrorInfo.Error;
                Assert.AreEqual("uh oh", errorObj["message"]);
                Assert.IsNull(ex.InnerException);
                Assert.IsNull(ex.ErrorInfo.ClrException);
            }
        }

        [Test]
        public void Test_toString()
        {
            using (var context = Engine.CreateContext())
            {
                context.RegisterHostObjectTemplate(new HostObjectTemplate(
                    toString: (IHostObjectCallbackContext ctx, object obj) => { return "alpha"; }
                ));

                var x = new object();
                context.SetVariable("x", x);
                Assert.AreEqual("alpha", context.Execute("x.toString()"));
            }
        }

        [Test]
        public void Test_toString_no_handler()
        {
            using (var context = Engine.CreateContext())
            {
                // No toString handler installed on template
                context.RegisterHostObjectTemplate(new HostObjectTemplate());

                var x = new object();
                context.SetVariable("x", x);
                Assert.AreEqual("[object Object]", context.Execute("x.toString()")); // default V8 implementation
            }
        }

        [Test]
        public void Test_toString_throws_exception()
        {
            var fooEx = new FooException("uh oh");
            using (var context = Engine.CreateContext())
            {
                context.RegisterHostObjectTemplate(new HostObjectTemplate(
                    toString: (IHostObjectCallbackContext ctx, object obj) => { throw fooEx; }
                ));

                var x = new object();
                context.SetVariable("x", x);

                var ex = Assert.Throws<JsException>(() =>
                {
                    context.Execute("x.toString()");
                });

                Assert.AreEqual(DefaultErrorName, ex.ErrorInfo.Name);
                var errorObj = (JsObject)ex.ErrorInfo.Error;
                Assert.AreEqual("uh oh", errorObj["message"]);
                Assert.AreSame(fooEx, ex.InnerException);
                Assert.AreSame(fooEx, ex.ErrorInfo.ClrException);
            }
        }

        [Test]
        public void Test_toString_throws_custom_error()
        {
            using (var context = Engine.CreateContext())
            {
                context.RegisterHostObjectTemplate(new HostObjectTemplate(
                    toString: (IHostObjectCallbackContext ctx, object obj)
                        => { throw new HostErrorException("uh oh", CustomErrorName); }
                ));

                var x = new object();
                context.SetVariable("x", x);

                var ex = Assert.Throws<JsException>(() =>
                {
                    context.Execute("x.toString()");
                });

                Assert.AreEqual(CustomErrorName, ex.ErrorInfo.Name);
                var errorObj = (JsObject)ex.ErrorInfo.Error;
                Assert.AreEqual("uh oh", errorObj["message"]);
                Assert.IsNull(ex.InnerException);
                Assert.IsNull(ex.ErrorInfo.ClrException);
            }
        }
    }
}
