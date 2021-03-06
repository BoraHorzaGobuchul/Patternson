﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patternson
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class PatternRecognitionTest
    {

        [TestMethod]
        public void SearchPatternTest1()
        {
            var patRecog = new PatternRecognition<char>();

            patRecog.IgnoreData.Add('.');
            var testData = "A.B.DC..AA.BCDA.BCDAA.B..C";

            var patTable = patRecog.SearchPattern(testData.ToList());
            var testResult = patTable.AsText();

            System.Diagnostics.Debug.Write(testResult);

            Assert.IsTrue(testResult.Contains("(0)A (11)A\nt: 8 9\n"));
            Assert.IsTrue(testResult.Contains("(0)A (2)B (3)C (4)D (5)A\nt: 9 14\n"));
            Assert.IsTrue(testResult.Contains("(0)A (6)A (8)B\nt: 8 14\n"));
            Assert.IsTrue(testResult.Contains("(0)A (17)C\nt: 0 8\n"));
            Assert.IsTrue(testResult.Contains("(0)A (2)B (4)D\nt: 0 9 14\n"));
            Assert.IsTrue(testResult.Contains("(0)A (1)A (3)B\nt: 8 19\n"));
            Assert.IsTrue(testResult.Contains("(0)C (3)A\nt: 5 17\n"));
            Assert.IsTrue(testResult.Contains("(0)A (2)B (5)C\nt: 0 20"));

            Assert.AreEqual(8, patTable.Count);
            
        }

        [TestMethod]
        public void SearchPatternTest2()
        {
            var patRecog = new PatternRecognition<char>();

            patRecog.IgnoreData.Add('.');

            var testData = new string[] {
                "AAA..A",
                "AA...."
            };
            var dataSources = new List<char>[] { testData[0].ToList(), testData[1].ToList() };

            var patTable = patRecog.SearchPattern(dataSources);

            Assert.AreEqual(0, patTable.SourceOffset[0]);
            Assert.AreEqual(6, patTable.SourceOffset[1]);

            var testResult = patTable.AsText();

            System.Diagnostics.Debug.Write(testResult);

            // already accomplished by SearchPattern()
            Assert.IsTrue(testResult.Contains("(0)A (1)A\nt: 0 1 6 \ns: 0"));

            // still above ability of SearchPattern (test-fails),
            // test formulates expection for the further development (test-driven development)
            Assert.IsTrue(testResult.Contains("(0)A (5)A\nt: 1 2 \ns: 1"));
            
            Assert.AreEqual(2, patTable.Count);

        }

    }
}
