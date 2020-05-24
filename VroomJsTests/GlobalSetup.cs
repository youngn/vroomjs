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
            var exePath = Assembly.GetAssembly(typeof(MarshalTests)).Location;
            JsEngine.Initialize(exePath);
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            JsEngine.Shutdown();
        }
    }
}
