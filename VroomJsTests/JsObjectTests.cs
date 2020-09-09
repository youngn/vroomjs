using NUnit.Framework;
using System;
using System.Linq;
using VroomJs;

namespace VroomJsTests
{
    [TestFixture]
    public class JsObjectTests : TestsBase
    {
        [Test]
        public void Test_Dispose()
        {
            using (var context = Engine.CreateContext())
            {
                var obj = (JsObject)context.Execute("({ a: 1, b: 2 })");

                obj.Dispose();
                // todo: how to verify that native resource was actually disposed?
            }
        }

        [Test]
        public void Test_Dispose_after_context()
        {
            JsObject obj;
            using (var context = Engine.CreateContext())
            {
                obj = (JsObject)context.Execute("({ a: 1, b: 2 })");
            }

            obj.Dispose();
        }

        [Test]
        public void Test_Dispose_after_engine()
        {
            JsObject obj;
            using (var engine = new JsEngine())
            using (var context = engine.CreateContext())
            {
                obj = (JsObject)context.Execute("({ a: 1, b: 2 })");
            }

            obj.Dispose();
        }

        // todo: this test isn't really testing anything
        [Test]
        public void Test_finalization()
        {
            using (var context = Engine.CreateContext())
            {
                // The delegate allows the local variable 'obj'
                // to be GC'd when GC.Collect() is called below.
                var isolatedScope = new Func<WeakReference>(() =>
                {
                    var obj = (JsObject)context.Execute("({ a: 1, b: 2 })");

                    var objWeakRef = new WeakReference(obj);
                    Assert.AreEqual(obj, objWeakRef.Target);

                    // Allow obj to go out of scope and be eligible
                    // for collection - return only weak ref
                    return objWeakRef;
                });

                var weakRef = isolatedScope();

                // Force collection so that locals refs used in the delegate invocation above
                // are collected.
                GC.Collect();

                Assert.IsNull(weakRef.Target); // assert GC actually collected it

                GC.WaitForPendingFinalizers();

                // todo: how to verify that native resource was actually disposed?
            }
        }

        [Test]
        [TestCase("a", 1)]
        [TestCase("a", 0)]
        [TestCase("b", "bob")]
        [TestCase("b", null)]
        [TestCase("d", false)]
        [TestCase("e", true)]
        [TestCase("a b", "bob")]
        [TestCase("", "bob")]
        public void Test_get_set_named_property(string name, object value)
        {
            using (var context = Engine.CreateContext())
            {
                // Test SetPropertyValue/GetPropertyValue methods
                var obj1 = (JsObject)context.Execute("({ })");
                obj1.SetPropertyValue(name, value);
                Assert.AreEqual(value, obj1.GetPropertyValue(name));

                // Test indexers
                var obj2 = (JsObject)context.Execute("({ })");
                obj2[name] = value;
                Assert.AreEqual(value, obj2[name]);

                // Test eval from script
                var obj4 = (JsObject)context.Execute("({ })");
                obj4.SetPropertyValue(name, value);
                context.SetVariable("x", obj4);
                Assert.AreEqual(value, context.Execute($"x['{name}']"));
            }
        }

        [Test]
        [TestCase(1)]
        [TestCase(0)]
        [TestCase("bob")]
        [TestCase(null)]
        [TestCase(false)]
        [TestCase(true)]
        public void Test_get_set_named_property_dynamic(object value)
        {
            using (var context = Engine.CreateContext())
            {
                // Test SetPropertyValue/GetPropertyValue methods
                dynamic obj1 = (JsObject)context.Execute("({ })");
                obj1.Alpha = value;
                Assert.AreEqual(value, obj1.Alpha);

                context.SetVariable("x", obj1);
                Assert.AreEqual(value, context.Execute("x.Alpha"));
            }
        }

        [Test]
        [TestCase(0, 1)]
        [TestCase(1, 0)]
        [TestCase(0, "bob")]
        [TestCase(1, null)]
        [TestCase(-1, false)]
        [TestCase(int.MaxValue, true)]
        [TestCase(int.MinValue, "bob")]
        public void Test_get_set_indexed_property(int index, object value)
        {
            using (var context = Engine.CreateContext())
            {
                // Test SetPropertyValue/GetPropertyValue methods
                var obj1 = (JsObject)context.Execute("({ })");
                obj1.SetPropertyValue(index, value);
                Assert.AreEqual(value, obj1.GetPropertyValue(index));

                // Test indexers
                var obj2 = (JsObject)context.Execute("({ })");
                obj2[index] = value;
                Assert.AreEqual(value, obj2[index]);

                // Test eval from script
                var obj4 = (JsObject)context.Execute("({ })");
                obj4.SetPropertyValue(index, value);
                context.SetVariable("x", obj4);
                Assert.AreEqual(value, context.Execute($"x[{index}]"));
            }
        }

        [Test]
        public void Test_InvokeMethod_named()
        {
            using (var context = Engine.CreateContext())
            {
                var obj = (JsObject)context.Execute("({ a: 1, b: function(x) { return x + this.a; } })");
                var result = obj.InvokeMethod("b", 3);

                Assert.AreEqual(4, result);
            }
        }

        [Test]
        public void Test_InvokeMethod_dynamic()
        {
            using (var context = Engine.CreateContext())
            {
                dynamic obj = context.Execute("({ a: 1, b: function(x) { return x + this.a; } })");
                var result = obj.b(3);

                Assert.AreEqual(4, result);
            }
        }

        [Test]
        public void Test_InvokeMethod_indexed()
        {
            using (var context = Engine.CreateContext())
            {
                var obj1 = (JsObject)context.Execute("({ a: 1, 2: function(x) { return x + this.a; } })");
                var result = obj1.InvokeMethod(2, 3);

                Assert.AreEqual(4, result);
            }
        }

        [Test]
        public void Test_InvokeMethod_invalid()
        {
            using (var context = Engine.CreateContext())
            {
                var obj = (JsObject)context.Execute("({ a: 1, 2: 'a' })");

                Assert.Throws<InvalidOperationException>(() => obj.InvokeMethod("a"));
                Assert.Throws<InvalidOperationException>(() => obj.InvokeMethod("b"));
                Assert.Throws<InvalidOperationException>(() => obj.InvokeMethod(1));
                Assert.Throws<InvalidOperationException>(() => obj.InvokeMethod(2));
            }
        }

        [Test]
        public void Test_GetPropertyNames()
        {
            using (var context = Engine.CreateContext())
            {
                var obj1 = (JsObject)context.Execute("({ a: 'bob', 1: 'cindy', b: 1, 2: 2 })");
                var propNames = obj1.GetPropertyNames();

                Assert.IsTrue(propNames.SequenceEqual(new object[] { 1, 2, "a", "b" }));
            }
        }
    }
}
