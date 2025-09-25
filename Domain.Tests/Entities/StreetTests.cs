using espasyo.Domain.Enums;

namespace espasyo.Domain.Entities.Tests
{
    [TestClass()]
    public class StreetTests
    {
        [TestMethod()]
        public void StreetTest()
        {
           var precinctId = Guid.NewGuid();
           var street = new Street(precinctId, "Test Street");
            Assert.IsNotNull(street);
        }

        [TestMethod()]
        public void StreetPropertiesTest()
        {
            var precinctId = Guid.NewGuid();
            var street = new Street(precinctId, "Test Street");
            Assert.AreEqual(precinctId, street.PrecinctId);
            Assert.AreEqual("Test Street", street.Name);
        }
    }
}