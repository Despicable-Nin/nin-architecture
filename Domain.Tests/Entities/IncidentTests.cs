using espasyo.Domain.Enums;

namespace espasyo.Domain.Entities.Tests
{
    [TestClass()]
    public class IncidentTests
    {
        private Incident incident = new(additionalInformation: "This is a test incident", address: "Test Address", caseId: "123", crimeType: CrimeTypeEnum.Homicide, motive: MotiveEnum.Anger, policeDistrictEnum: Barangay.Alabang, severity: SeverityEnum.High, timeStamp: DateTimeOffset.Now, weatherCondition: WeatherConditionEnum.Clear);

        public IncidentTests()
        {
           
        }

        [TestMethod()]
        public void IncidentTest()
        {
            Assert.IsNotNull(incident);
        }

        [TestMethod()]
        public void ChangeLatLongTest()
        {
            var lat = incident.GetLatitude();
            var lng = incident.GetLongitude();

            incident.ChangeLatLong(14.123, 121.123);
            Assert.AreEqual(0, lat);
            Assert.AreEqual(0, lng);
            Assert.AreNotEqual(lat, incident.GetLatitude());
            Assert.AreNotEqual(lng, incident.GetLongitude());
            Assert.AreEqual(14.123, incident.GetLatitude());
            Assert.AreEqual(121.123, incident.GetLongitude());
        }

        [TestMethod()]
        public void SanitizeAddressTest()
        {
            var oldAddress = incident.SanitizedAddress;
            incident.SanitizeAddress("New Address");
            Assert.AreEqual("New Address", incident.SanitizedAddress);
        }

        [TestMethod()]
        public void IsAnomalyTest()
        {
            Assert.IsTrue(incident.IsAnomaly());
        }

        [TestMethod()]
        public void GetYearTest()
        {
            Assert.AreEqual(2025, incident.GetYear());
        }

        [TestMethod()]
        public void GetMonthTest()
        {
            Assert.AreEqual(DateTimeOffset.Now.Month, incident.GetMonth());
        }

        [TestMethod()]
        public void GetTimeOfDayTest()
        {
            DateTimeOffset time = incident!.TimeStamp!.Value;
            var temp = time.Hour switch
            {
                >= 0 and < 12 => "Morning",
                >= 12 and < 18 => "Afternoon",
                _ => "Evening"
            };
            Assert.AreEqual(temp, incident.GetTimeOfDay());
        }
        [TestMethod()]
        public void PoliceDistrictTest()
        {
            // Arrange
            var expectedDistrict = Barangay.Alabang;

            // Act
            var actualDistrict = incident.PoliceDistrict;

            // Assert
            Assert.AreEqual(expectedDistrict, actualDistrict);
        }
    }
}