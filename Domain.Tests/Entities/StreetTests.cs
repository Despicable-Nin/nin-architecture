using Microsoft.VisualStudio.TestTools.UnitTesting;
using espasyo.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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