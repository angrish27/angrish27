using CS.AgenticUrlShortner.Shared.Tests;
using CS.AgenticUrlShortner.Utilities.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CS.AgenticUrlShortner.Tests.UtilityTests
{
    [TestClass]
    public class Base62UtilityTests : TestBase
    {
        [TestMethod]
        [DataRow(0L, "0")]
        [DataRow(61L, "z")]
        [DataRow(62L, "10")]
        [DataRow(11157L, "2tx")]
        [DataRow(11158L, "2ty")]
        public void Base62Utility_Encode_ReturnsExpected(long value, string expected)
        {
            Assert.AreEqual(expected, UtilityFactory.Create<IBase62Utility>().Encode(value));
        }

        [TestMethod]
        [DataRow("2tx", true)]
        [DataRow("", false)]
        [DataRow(null, false)]
        [DataRow("2t-x", false)]
        public void Base62Utility_IsValid_ReturnsExpected(string value, bool expected)
        {
            Assert.AreEqual(expected, UtilityFactory.Create<IBase62Utility>().IsValid(value));
        }
    }
}
