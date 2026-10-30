using CS.AgenticUrlShortner.Shared.Tests;
using CS.AgenticUrlShortner.Utilities.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CS.AgenticUrlShortner.Tests.UtilityTests
{
    [TestClass]
    public class IdentifierUtilityTests : TestBase
    {
        [TestMethod]
        public void IdentifierUtility_NewId_ReturnsUniqueValues()
        {
            var utility = UtilityFactory.Create<IIdentifierUtility>();

            Assert.AreNotEqual(utility.NewId(), utility.NewId());
        }
    }
}
