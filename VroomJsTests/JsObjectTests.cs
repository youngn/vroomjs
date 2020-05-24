using NUnit.Framework;
using System;
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
    }
}
