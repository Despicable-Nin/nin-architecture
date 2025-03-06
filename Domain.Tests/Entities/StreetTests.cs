using espasyo.Domain.Enums;

namespace espasyo.Domain.Entities.Tests
{
    [TestClass()]
    public class StreetTests
    {
        [TestMethod()]
        public void StreetTest()
        {
           var street = new Street(Barangay.Alabang, "Test Street");
            Assert.IsNotNull(street);
        }

        [TestMethod()]
        public void GetBarangayTest()
        {
            var street = new Street(Barangay.Alabang, "Test Street");
            Assert.AreEqual(Barangay.Alabang, street.GetBarangay());
        }
    }
}