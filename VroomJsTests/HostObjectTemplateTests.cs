using NUnit.Framework;
using System;
using System.Linq;
using VroomJs;

namespace VroomJsTests
{
    [TestFixture]
    class HostObjectTemplateTests
    {
        [Test]
        public void Test_named_property()
        {
            object propValue = null;
            using (var engine = new JsEngine())
            {
                // Property handlers are installed and return true (handled)
                engine.RegisterHostObjectTemplate(new HostObjectTemplate(
                    tryGetProperty: (IHostObjectCallbackContext ctx, object obj, string name, out object value) => { value = propValue; return true; },
                    trySetProperty: (IHostObjectCallbackContext ctx, object obj, string name, object value) => { propValue = value; return true; },
                    tryDeleteProperty: (IHostObjectCallbackContext ctx, object obj, string name, out bool deleted) => { propValue = null; deleted = true; return true; }
                ));

                using (var context = engine.CreateContext())
                {
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
        }

        [Test]
        public void Test_named_property_no_handler()
        {
            using(var engine = new JsEngine())
            {
                // No get/set handler is installed on the template
                engine.RegisterHostObjectTemplate(new HostObjectTemplate());

                using (var context = engine.CreateContext())
                {
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
        }

        [Test]
        public void Test_named_property_not_handled()
        {
            using (var engine = new JsEngine())
            {
                // Property handlers are installed, but return false (not handled)
                engine.RegisterHostObjectTemplate(new HostObjectTemplate(
                    tryGetProperty: (IHostObjectCallbackContext ctx, object obj, string name, out object value) => { value = null; return false; },
                    trySetProperty: (IHostObjectCallbackContext ctx, object obj, string name, object value) => { return false; },
                    tryDeleteProperty: (IHostObjectCallbackContext ctx, object obj, string name, out bool deleted) => { deleted = false; return false; }
                ));

                using (var context = engine.CreateContext())
                {
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
        }

        [Test]
        public void Test_get_property_throws_exception()
        {
            using (var engine = new JsEngine())
            {
                // Property handlers are installed and return true (handled)
                engine.RegisterHostObjectTemplate(new HostObjectTemplate(
                    tryGetProperty: (IHostObjectCallbackContext ctx, object obj, string name, out object value) => { throw new Exception("uh oh"); }
                ));

                using (var context = engine.CreateContext())
                {
                    var x = new object();
                    context.SetVariable("x", x);

                    var result = context.Execute("try { x.bar; } catch(e) { e; }") as Exception;
                    Assert.IsNotNull(result);
                    Assert.AreEqual("uh oh", result.Message);
                }
            }
        }

        [Test]
        public void Test_set_property_throws_exception()
        {
            using (var engine = new JsEngine())
            {
                // Property handlers are installed and return true (handled)
                engine.RegisterHostObjectTemplate(new HostObjectTemplate(
                    trySetProperty: (IHostObjectCallbackContext ctx, object obj, string name, object value) => { throw new Exception("uh oh"); }
                ));

                using (var context = engine.CreateContext())
                {
                    var x = new object();
                    context.SetVariable("x", x);

                    var result = context.Execute("try { x.bar = 1; } catch(e) { e; }") as Exception;
                    Assert.IsNotNull(result);
                    Assert.AreEqual("uh oh", result.Message);
                }
            }
        }

        [Test]
        public void Test_delete_property_throws_exception()
        {
            using (var engine = new JsEngine())
            {
                // Property handlers are installed and return true (handled)
                engine.RegisterHostObjectTemplate(new HostObjectTemplate(
                    tryDeleteProperty: (IHostObjectCallbackContext ctx, object obj, string name, out bool deleted) => { throw new Exception("uh oh"); }
                ));

                using (var context = engine.CreateContext())
                {
                    var x = new object();
                    context.SetVariable("x", x);

                    var result = context.Execute("try { delete x.bar; } catch(e) { e; }") as Exception;
                    Assert.IsNotNull(result);
                    Assert.AreEqual("uh oh", result.Message);
                }
            }
        }

        [Test]
        public void Test_enumerate_properties()
        {
            using (var engine = new JsEngine())
            {
                engine.RegisterHostObjectTemplate(new HostObjectTemplate(
                    enumerateProperties: (IHostObjectCallbackContext ctx, object obj) => { return new string[] { "alpha", "beta", "gamma" }; }
                ));

                using (var context = engine.CreateContext())
                {
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
        }

        [Test]
        public void Test_enumerate_properties_throws_exception()
        {
            using (var engine = new JsEngine())
            {
                engine.RegisterHostObjectTemplate(new HostObjectTemplate(
                    enumerateProperties: (IHostObjectCallbackContext ctx, object obj) => { throw new Exception("uh oh"); }
                ));

                using (var context = engine.CreateContext())
                {
                    var x = new object();
                    context.SetVariable("x", x);

                    var result = context.Execute("try { Object.keys(x); } catch(e) { e; }") as Exception;
                    Assert.IsNotNull(result);
                    Assert.AreEqual("uh oh", result.Message);
                }
            }
        }

        [Test]
        public void Test_invoke()
        {
            using (var engine = new JsEngine())
            {
                engine.RegisterHostObjectTemplate(new HostObjectTemplate(
                    invoke: (IHostObjectCallbackContext ctx, object obj, object[] args) => { return args.Sum(x => (int)x); }
                ));

                using (var context = engine.CreateContext())
                {
                    var f = new object();
                    context.SetVariable("f", f);

                    Assert.IsTrue((bool)context.Execute("f() === 0"));
                    Assert.IsTrue((bool)context.Execute("f(1) === 1"));
                    Assert.IsTrue((bool)context.Execute("f(1, 2) === 3"));
                    Assert.IsTrue((bool)context.Execute("f(1, 2, 3) === 6"));
                }
            }
        }

        [Test]
        public void Test_invoke_returns_undefined()
        {
            using (var engine = new JsEngine())
            {
                engine.RegisterHostObjectTemplate(new HostObjectTemplate(
                    invoke: (IHostObjectCallbackContext ctx, object obj, object[] args) => { return JsUndefined.Value; }
                ));

                using (var context = engine.CreateContext())
                {
                    var f = new object();
                    context.SetVariable("f", f);

                    Assert.IsTrue((bool)context.Execute("f() === undefined"));
                    Assert.IsFalse((bool)context.Execute("f() === null"));
                }
            }
        }

        [Test]
        public void Test_invoke_returns_null()
        {
            using (var engine = new JsEngine())
            {
                engine.RegisterHostObjectTemplate(new HostObjectTemplate(
                    invoke: (IHostObjectCallbackContext ctx, object obj, object[] args) => { return null; }
                ));

                using (var context = engine.CreateContext())
                {
                    var f = new object();
                    context.SetVariable("f", f);

                    Assert.IsTrue((bool)context.Execute("f() === null"));
                    Assert.IsFalse((bool)context.Execute("f() === undefined"));
                }
            }
        }

        [Test]
        public void Test_invoke_throws_exception()
        {
            using (var engine = new JsEngine())
            {
                engine.RegisterHostObjectTemplate(new HostObjectTemplate(
                    invoke: (IHostObjectCallbackContext ctx, object obj, object[] args) => { throw new Exception("uh oh"); }
                ));

                using (var context = engine.CreateContext())
                {
                    var f = new object();
                    context.SetVariable("f", f);

                    var result = context.Execute("try { f(); } catch(e) { e; }") as Exception;
                    Assert.IsNotNull(result);
                    Assert.AreEqual("uh oh", result.Message);
                }
            }
        }

        [Test]
        public void Test_valueOf()
        {
            using (var engine = new JsEngine())
            {
                engine.RegisterHostObjectTemplate(new HostObjectTemplate(
                    valueOf: (IHostObjectCallbackContext ctx, object obj) => { return 1; }
                ));

                using (var context = engine.CreateContext())
                {
                    var x = new object();
                    context.SetVariable("x", x);
                    Assert.AreEqual(1, context.Execute("x.valueOf()"));
                }
            }
        }

        [Test]
        public void Test_valueOf_no_handler()
        {
            using (var engine = new JsEngine())
            {
                // No valueOf handler installed on template
                engine.RegisterHostObjectTemplate(new HostObjectTemplate());

                using (var context = engine.CreateContext())
                {
                    var x = new object();
                    context.SetVariable("x", x);
                    Assert.AreEqual(x, context.Execute("x.valueOf()")); // default V8 implementation
                }
            }
        }

        [Test]
        public void Test_valueOf_throws_exception()
        {
            using (var engine = new JsEngine())
            {
                engine.RegisterHostObjectTemplate(new HostObjectTemplate(
                    valueOf: (IHostObjectCallbackContext ctx, object obj) => { throw new Exception("uh oh"); }
                ));

                using (var context = engine.CreateContext())
                {
                    var x = new object();
                    context.SetVariable("x", x);

                    var result = context.Execute("try { x.valueOf(); } catch(e) { e; }") as Exception;
                    Assert.IsNotNull(result);
                    Assert.AreEqual("uh oh", result.Message);
                }
            }
        }

        [Test]
        public void Test_toString()
        {
            using (var engine = new JsEngine())
            {
                engine.RegisterHostObjectTemplate(new HostObjectTemplate(
                    toString: (IHostObjectCallbackContext ctx, object obj) => { return "alpha"; }
                ));

                using (var context = engine.CreateContext())
                {
                    var x = new object();
                    context.SetVariable("x", x);
                    Assert.AreEqual("alpha", context.Execute("x.toString()"));
                }
            }
        }

        [Test]
        public void Test_toString_no_handler()
        {
            using (var engine = new JsEngine())
            {
                // No toString handler installed on template
                engine.RegisterHostObjectTemplate(new HostObjectTemplate());

                using (var context = engine.CreateContext())
                {
                    var x = new object();
                    context.SetVariable("x", x);
                    Assert.AreEqual("[object Object]", context.Execute("x.toString()")); // default V8 implementation
                }
            }
        }

        [Test]
        public void Test_toString_throws_exception()
        {
            using (var engine = new JsEngine())
            {
                engine.RegisterHostObjectTemplate(new HostObjectTemplate(
                    toString: (IHostObjectCallbackContext ctx, object obj) => { throw new Exception("uh oh"); }
                ));

                using (var context = engine.CreateContext())
                {
                    var x = new object();
                    context.SetVariable("x", x);

                    var result = context.Execute("try { x.toString(); } catch(e) { e; }") as Exception;
                    Assert.IsNotNull(result);
                    Assert.AreEqual("uh oh", result.Message);
                }
            }
        }
    }
}
