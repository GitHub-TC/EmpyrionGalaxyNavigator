using System.IO;
using EmpyrionGalaxyNavigator;
using EmpyrionNetAPITools.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public void TestMethodReadDynGalaxyMap()
        {
            var map = new GalaxyMap();
            map.ReadSectors(@"..\..");

            Assert.AreEqual(4, map.Navigate("Sunchodacil", "Kai Moon").Count);
        }

        [TestMethod]
        public void TestMethodReadGalaxyMap()
        {
            var map = new GalaxyMap();
            map.ReadSectorsData(File.ReadAllText(@"simplesectors.yaml"));

            Assert.IsTrue(map.Exists("Lloelli Orbit"));
        }

        [TestMethod]
        public void TestMethodNavigateOrbitToOrbit()
        {
            var map = new GalaxyMap();
            map.ReadSectorsData(File.ReadAllText(@"simplesectors.yaml"));

            Assert.AreEqual(4, map.Navigate("Lloelli Orbit", "Olos Orbit").Count);
        }

        [TestMethod]
        public void TestMethodNavigateOrbitToFarOrbit()
        {
            var map = new GalaxyMap();
            map.ReadSectorsData(File.ReadAllText(@"simplesectors.yaml"));

            Assert.AreEqual(3, map.Navigate("Olos Orbit", "Enaz Gamma Station").Count);
        }

        [TestMethod]
        public void TestMethodNavigateOrbitToNothing()
        {
            var map = new GalaxyMap();
            map.ReadSectorsData(File.ReadAllText(@"simplesectors.yaml"));

            Assert.AreEqual(1, map.Navigate("Olos Orbit", "Ark of Life").Count);
        }

        [TestMethod]
        public void TestMethodNavigatePlanetToFarPlanet()
        {
            var map = new GalaxyMap();
            map.ReadSectorsData(File.ReadAllText(@"simplesectors.yaml"));

            Assert.AreEqual(1, map.Navigate("Olos", "Lodra").Count);
        }

        [TestMethod]
        public void TestMethodNavigatePlanetToPlanetSameOrbit()
        {
            var map = new GalaxyMap();
            map.ReadSectorsData(File.ReadAllText(@"simplesectors.yaml"));

            Assert.AreEqual(3, map.Navigate("Buoll", "Buoll Moon").Count);
        }

        [TestMethod]
        public void TestMethodNavigateAgain()
        {
            var map = new GalaxyMap();
            map.ReadSectorsData(File.ReadAllText(@"simplesectors.yaml"));

            Assert.AreEqual(3, map.Navigate("Buoll", "Buoll Moon").Count);
            Assert.AreEqual(2, map.Navigate("Buoll Orbit", "Buoll Moon").Count);
        }

        [TestMethod]
        public void TestMethodNavigateOrbitToFarOrbit2()
        {
            var map = new GalaxyMap();
            map.ReadSectorsData(File.ReadAllText(@"simplesectors.yaml"));

            Assert.AreEqual(7, map.Navigate("Mukund", "Aliens").Count);
        }

    }
}
