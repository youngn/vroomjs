using System;
using VroomJs;
using NUnit.Framework;
using System.Linq;

namespace VroomJsTests
{
    [TestFixture]
    class HostErrorInfoTests : TestsBase
    {
        class FooException : Exception
        {
            public FooException(string message)
                :base(message)
            {
            }
        }

        private void AssertKeys(JsObject errorObj, params string[] expectedKeys)
        {
            var context = errorObj.Context;
            context.SetVariable("x", errorObj);
            var keys = ((JsArray)context.Execute("Object.keys(x)")).ToList();
            Assert.AreEqual(expectedKeys.Length, keys.Count);
            foreach (var key in expectedKeys)
                Assert.Contains(key, keys);
        }

        [Test]
        public void Test_error_without_exception()
        {
            var errorInfo = new HostErrorInfo(name: "FooError", message: "Too many foos in the bar.");
            using (var context = Engine.CreateContext())
            {
                var errorObj = errorInfo.ToErrorObject(context);
                Assert.AreEqual("FooError", errorObj["name"]);
                Assert.AreEqual("Too many foos in the bar.", errorObj["message"]);

                AssertKeys(errorObj, "name", "message");
            }
        }

        [Test]
        public void Test_error_without_exception_with_custom_properties()
        {
            var errorInfo = new HostErrorInfo(name: "FooError", message: "Too many foos in the bar.");
            errorInfo["alpha"] = "bog";
            errorInfo["beta"] = 10;
            using (var context = Engine.CreateContext())
            {
                var errorObj = errorInfo.ToErrorObject(context);
                Assert.AreEqual("FooError", errorObj["name"]);
                Assert.AreEqual("Too many foos in the bar.", errorObj["message"]);
                Assert.AreEqual("bog", errorObj["alpha"]);
                Assert.AreEqual(10, errorObj["beta"]);

                AssertKeys(errorObj, "name", "message", "alpha", "beta");
            }
        }

        [Test]
        public void Test_error_from_exception()
        {
            var ex = new FooException("Too many foos in the bar.");
            var errorInfo = new HostErrorInfo(ex);
            using (var context = Engine.CreateContext())
            {
                var errorObj = errorInfo.ToErrorObject(context);
                Assert.AreEqual("Error", errorObj["name"]);
                Assert.AreEqual("Too many foos in the bar.", errorObj["message"]);

                AssertKeys(errorObj, "name", "message", "toString"); // todo: toString should not be enumerated

                // todo: verify that the object is actually a proxy around the exception object
            }
        }

        [Test]
        public void Test_error_from_exception_with_custom_name_and_message()
        {
            var ex = new FooException("Too many foos in the bar.");
            var errorInfo = new HostErrorInfo(ex, name: "FooError", message: "Some other message.");
            using (var context = Engine.CreateContext())
            {
                var errorObj = errorInfo.ToErrorObject(context);
                Assert.AreEqual("FooError", errorObj["name"]);
                Assert.AreEqual("Some other message.", errorObj["message"]);

                AssertKeys(errorObj, "name", "message", "toString");// todo: toString should not be enumerated

                // todo: verify that the object is actually a proxy around the exception object
            }
        }

        [Test]
        public void Test_error_from_exception_with_custom_properties()
        {
            var ex = new FooException("Too many foos in the bar.");
            var errorInfo = new HostErrorInfo(ex);
            errorInfo["alpha"] = "bog";
            errorInfo["beta"] = 10;
            using (var context = Engine.CreateContext())
            {
                var errorObj = errorInfo.ToErrorObject(context);
                Assert.AreEqual("Error", errorObj["name"]);
                Assert.AreEqual("Too many foos in the bar.", errorObj["message"]);
                Assert.AreEqual("bog", errorObj["alpha"]);
                Assert.AreEqual(10, errorObj["beta"]);

                AssertKeys(errorObj, "name", "message", "alpha", "beta", "toString");// todo: toString should not be enumerated
            }
        }

        [Test]
        public void Test_ConvertException()
        {
            var ex = new FooException("Too many foos in the bar.");
            var errorInfo = HostErrorInfo.ConvertException(ex);

            Assert.AreEqual(ex, errorInfo.Exception);
            Assert.AreEqual(null, errorInfo.Name);
            Assert.AreEqual(null, errorInfo.Message);
        }

        [Test]
        public void Test_ConvertException_HostErrorException_without_underlying_exception()
        {
            var ex = new HostErrorException("Too many foos in the bar.", "FooError");
            var errorInfo = HostErrorInfo.ConvertException(ex);

            Assert.AreEqual(null, errorInfo.Exception);
            Assert.AreEqual("FooError", errorInfo.Name);
            Assert.AreEqual("Too many foos in the bar.", errorInfo.Message);
        }

        [Test]
        public void Test_ConvertException_HostErrorException_with_underlying_exception()
        {
            var ex = new FooException("Too many foos in the bar.");
            var ex1 = new HostErrorException(ex);
            var errorInfo = HostErrorInfo.ConvertException(ex1);

            Assert.AreEqual(ex, errorInfo.Exception);
            Assert.AreEqual(null, errorInfo.Name);
            Assert.AreEqual(null, errorInfo.Message);
        }

        [Test]
        public void Test_ConvertException_HostErrorException_with_underlying_exception_and_custom_name_and_message()
        {
            var ex = new FooException("Too many foos in the bar.");
            var ex1 = new HostErrorException(ex, errorName: "FooError", message: "Some other message.");
            var errorInfo = HostErrorInfo.ConvertException(ex1);

            Assert.AreEqual(ex, errorInfo.Exception);
            Assert.AreEqual("FooError", errorInfo.Name);
            Assert.AreEqual("Some other message.", errorInfo.Message);
        }
    }
}
