using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using VroomJs;

namespace VroomJsTests
{
    [TestFixture]
    public class JsArrayTests : TestsBase
    {
        [Test]
        [TestCase("[]", 0)]
        [TestCase("[1, 2, 3]", 3)]
        [TestCase("var x=[]; x[1] = 1, x[4] = 2; x", 5)]
        [TestCase("var x=[]; x[-1] = 1, x[1] = 2; x", 2)]
        public void Test_GetLength(string script, int expectedLength)
        {
            using (var context = Engine.CreateContext())
            {
                var arr = (JsArray)context.Execute(script);
                Assert.AreEqual(expectedLength, arr.GetLength());
            }
        }

        [Test]
        [TestCaseSource(nameof(TestCases_enumeration))]
        public object Test_enumeration(string script)
        {
            using (var context = Engine.CreateContext())
            {
                var arr = (JsArray)context.Execute(script);
                return arr.ToArray();
            }
        }

        private static IEnumerable<TestCaseData> TestCases_enumeration()
        {
            yield return new TestCaseData("[]").Returns(new object[] {});
            yield return new TestCaseData("['a']").Returns(new object[] { "a" });
            yield return new TestCaseData("['a', 'b', 'c']").Returns(new object[] { "a", "b", "c" });
            yield return new TestCaseData("[1, 'b', true]").Returns(new object[] { 1, "b", true });

            yield return new TestCaseData("var x=[]; x[1] = 1, x[4] = 2; x").Returns(new object[] { null, 1, null, null, 2 });
            yield return new TestCaseData("var x=[]; x[-1] = 1, x[1] = 2; x").Returns(new object[] { null, 2 });
        }
    }
}
