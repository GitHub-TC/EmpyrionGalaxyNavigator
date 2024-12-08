using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using EmpyrionGalaxyNavigator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public void TestMethodMap()
        {
            Node endNode, startNode;

            var map = new Map();
            map.Nodes.Add("A", startNode = new Node() { Name = "A", Coordinates = new Vector3(0, 0, 0) });
            map.Nodes.Add("Z", endNode   = new Node() { Name = "Z", Coordinates = new Vector3(100, 100, 100) });

            map.Nodes.Add("X0", new Node() { Name = "X0", Coordinates = new Vector3(10, 10, 10) });
            map.Nodes.Add("X1", new Node() { Name = "X1", Coordinates = new Vector3(25, 25, 25) });
            map.Nodes.Add("X2", new Node() { Name = "X2", Coordinates = new Vector3(50, 50, 50) });
            map.Nodes.Add("X3", new Node() { Name = "X3", Coordinates = new Vector3(75, 75, 75) });

            map.Nodes.Add("Y0", new Node() { Name = "Y0", Coordinates = new Vector3(10, 0, 10) });
            map.Nodes.Add("Y1", new Node() { Name = "Y1", Coordinates = new Vector3(20, 0, 10) });
            map.Nodes.Add("Y2", new Node() { Name = "Y2", Coordinates = new Vector3(30, 0, 10) });
            map.Nodes.Add("Y3", new Node() { Name = "Y3", Coordinates = new Vector3(40, 0, 10) });
            map.Nodes.Add("Y4", new Node() { Name = "Y4", Coordinates = new Vector3(50, 0, 10) });
            map.Nodes.Add("Y5", new Node() { Name = "Y5", Coordinates = new Vector3(60, 0, 10) });
            map.Nodes.Add("Y6", new Node() { Name = "Y6", Coordinates = new Vector3(70, 0, 10) });
            map.Nodes.Add("Y7", new Node() { Name = "Y7", Coordinates = new Vector3(80, 0, 10) });
            map.Nodes.Add("Y8", new Node() { Name = "Y8", Coordinates = new Vector3(90, 0, 10) });
            map.Nodes.Add("Y9", new Node() { Name = "Y9", Coordinates = new Vector3(90, 0, 20) });
            map.Nodes.Add("Y10", new Node() { Name = "Y10", Coordinates = new Vector3(90, 0, 30) });
            map.Nodes.Add("Y11", new Node() { Name = "Y11", Coordinates = new Vector3(90, 0, 40) });
            map.Nodes.Add("Y12", new Node() { Name = "Y12", Coordinates = new Vector3(90, 0, 50) });
            map.Nodes.Add("Y13", new Node() { Name = "Y13", Coordinates = new Vector3(90, 0, 60) });
            map.Nodes.Add("Y14", new Node() { Name = "Y14", Coordinates = new Vector3(90, 0, 70) });
            map.Nodes.Add("Y15", new Node() { Name = "Y15", Coordinates = new Vector3(90, 0, 80) });
            map.Nodes.Add("Y16", new Node() { Name = "Y16", Coordinates = new Vector3(90, 0, 90) });
            map.Nodes.Add("Y17", new Node() { Name = "Y17", Coordinates = new Vector3(90, 10, 90) });
            map.Nodes.Add("Y18", new Node() { Name = "Y18", Coordinates = new Vector3(90, 20, 90) });
            map.Nodes.Add("Y19", new Node() { Name = "Y19", Coordinates = new Vector3(90, 30, 90) });
            map.Nodes.Add("Y20", new Node() { Name = "Y20", Coordinates = new Vector3(90, 40, 90) });
            map.Nodes.Add("Y21", new Node() { Name = "Y21", Coordinates = new Vector3(90, 50, 90) });
            map.Nodes.Add("Y22", new Node() { Name = "Y22", Coordinates = new Vector3(90, 60, 90) });
            map.Nodes.Add("Y23", new Node() { Name = "Y23", Coordinates = new Vector3(90, 70, 90) });
            map.Nodes.Add("Y24", new Node() { Name = "Y24", Coordinates = new Vector3(90, 80, 90) });
            map.Nodes.Add("Y25", new Node() { Name = "Y25", Coordinates = new Vector3(90, 90, 90) });

            var search = new SearchEngine(map)
            {
                Start = startNode,
                End   = endNode
            };

            Assert.AreEqual("A -> Y0 -> Y1 -> Y3 -> Y5 -> Y7 -> Y9 -> Y11 -> Y13 -> Y15 -> Y17 -> Y19 -> Y21 -> Y23 -> Y25 -> Z", search.GetShortestPathAstart( 20).Aggregate((string)null, (s, n) => s == null ? n.Name : $"{s} -> {n.Name}"));
            Assert.AreEqual("A -> X1 -> X2 -> X3 -> Z",                                                                           search.GetShortestPathAstart( 50).Aggregate((string)null, (s, n) => s == null ? n.Name : $"{s} -> {n.Name}"));
            Assert.AreEqual("A -> X2 -> Z",                                                                                       search.GetShortestPathAstart(100).Aggregate((string)null, (s, n) => s == null ? n.Name : $"{s} -> {n.Name}"));
            Assert.AreEqual("A -> Z",                                                                                             search.GetShortestPathAstart(200).Aggregate((string)null, (s, n) => s == null ? n.Name : $"{s} -> {n.Name}"));
        }

        [TestMethod]
        public void TestMethodTravelTestMapNew()
        {
            var map = new GalaxyMap();

            map.ReadDbData(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\global-new.db"));

            Assert.AreEqual("Naamaromi [34 LY] -> Alpha [31 LY] -> Akua [0 LY]", map.Navigate("Adunti", "Akua", 40 * Const.SectorsPerLY).Aggregate((string)null, (s, n) => s == null ? n.ToString() : $"{s} -> {n}"));
        }


        [TestMethod]
        public void TestMethodTravelTestMap()
        {
            var map = new GalaxyMap();

            map.ReadDbData(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\global.db"));

            Assert.AreEqual("Beta [5 LY]", map.Navigate("Alpha", "Beta", 50 * Const.SectorsPerLY).Aggregate((string)null, (s, n) => s == null ? n.ToString() : $"{s} -> {n}"));
            Assert.AreEqual("Naphona [43 LY] -> Cyrimudai [17 LY] -> Malphukaltho [49 LY] -> Rangudra [39 LY] -> Naamadhaka [45 LY] -> Mastiless [49 LY] -> Farr [49 LY]", map.Navigate("Alpha", "Farr",  50 * Const.SectorsPerLY).Aggregate((string)null, (s, n) => s == null ? n.ToString() : $"{s} -> {n}"));
        }

        [TestMethod]
        public void TestMethodTravelSolarToPlanetTestMap()
        {
            var map = new GalaxyMap();

            map.ReadDbData(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\global.db"));

            Assert.AreEqual("Beta [5 LY] -> Omicron [0 LY]", map.Navigate("Alpha", "omicron", 50 * Const.SectorsPerLY).Aggregate((string)null, (s, n) => s == null ? n.ToString() : $"{s} -> {n}"));
            Assert.AreEqual("Beta [5 LY] -> Omicron [0 LY]", map.Navigate("A lpha", "Omicron", 50 * Const.SectorsPerLY).Aggregate((string)null, (s, n) => s == null ? n.ToString() : $"{s} -> {n}"));
        }

        [TestMethod]
        public void TestMethodTravelPlanetToPlanetTestMap()
        {
            var map = new GalaxyMap();

            map.ReadDbData(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\global.db"));

            Assert.AreEqual("Beta [5 LY] -> Omicron [0 LY]", map.Navigate("Haven", "Omicron", 50 * Const.SectorsPerLY).Aggregate((string)null, (s, n) => s == null ? n.ToString() : $"{s} -> {n}"));
        }

        [TestMethod]
        public void TestMethodTravelPlanetToSolarTestMap()
        {
            var map = new GalaxyMap();

            map.ReadDbData(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\global.db"));

            Assert.AreEqual("Beta [5 LY]", map.Navigate("Haven", "Beta", 50 * Const.SectorsPerLY).Aggregate((string)null, (s, n) => s == null ? n.ToString() : $"{s} -> {n}"));
        }
    }
}