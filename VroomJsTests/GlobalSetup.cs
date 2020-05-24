using NUnit.Framework;
using System.Reflection;
using VroomJs;

namespace VroomJsTests
{
    [SetUpFixture]
    public class GlobalSetup
    {
        [OneTimeSetUp]
        public void Setup()
        {
            JsEngine.Initialize();
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            JsEngine.Shutdown();
        }
    }
}
