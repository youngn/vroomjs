using NUnit.Framework;
using System.Reflection;
using VroomJs;

namespace VroomJsTests
{
    public abstract class TestsBase
    {
        private JsEngine _engine;

        [OneTimeSetUp]
        public void FixtureSetup()
        {
            _engine = new JsEngine();
        }

        [OneTimeTearDown]
        public void FixtureTeardown()
        {
            _engine.Dispose();
        }

        protected JsEngine Engine => _engine;
    }
}
