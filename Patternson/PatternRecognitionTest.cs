using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            var patRecog = new PatternRecognition();

            patRecog.IgnoreData.Add(Convert.ToByte('.'));


            var testString = "A.B.DC..AA.BCDA.BCDAA.B..C";

            var testData = new List<byte>();

            foreach (char c in testString.ToCharArray())
                testData.Add((byte)(c));

            var patTable = patRecog.SearchPattern(testData.ToArray<byte>());

            var testResult = patTable.AsText();

            Assert.IsTrue(testResult.Contains("(0,0)A (0,11)A\nt: 8 9\n"));
            Assert.IsTrue(testResult.Contains("(0,0)A (0,2)B (0,3)C (0,4)D (0,5)A\nt: 9 14\n"));
            Assert.IsTrue(testResult.Contains("(0,0)A (0,6)A (0,8)B\nt: 8 14\n"));
            Assert.IsTrue(testResult.Contains("(0,0)A (0,17)C\nt: 0 8\n"));
            Assert.IsTrue(testResult.Contains("(0,0)A (0,2)B (0,4)D\nt: 0 9 14\n"));
            Assert.IsTrue(testResult.Contains("(0,0)A (0,1)A (0,3)B\nt: 8 19\n"));
            Assert.IsTrue(testResult.Contains("(0,0)C (0,3)A\nt: 5 17\n"));
            Assert.IsTrue(testResult.Contains("(0,0)A (0,2)B (0,5)C\nt: 0 20"));

            Assert.AreEqual(8, patTable.Count);
            
        }

        [TestMethod]
        public void SearchPatternTest2()
        {
            // Assert.Inconclusive("Implementation of test method not completed.");

            var patRecog = new PatternRecognition();

            patRecog.IgnoreData.Add(Convert.ToByte('.'));

            var testString = new string[] {
                "AAA..A",
                "AA...."
            };

            var testData0 = new List<byte>();
            var testData1 = new List<byte>();


            foreach (char c in testString[0].ToCharArray())
                testData0.Add((byte)(c));

            foreach (char c in testString[1].ToCharArray())
                testData1.Add((byte)(c));


            var patTable = patRecog.SearchPattern(
                testData0.ToArray<byte>(),
                testData1.ToArray<byte>());

            var testResult = patTable.AsText();

            Assert.IsTrue(testResult.Contains("(0,0)A (0,1)A (1,6)A\nt: 0 1"));
            
            Assert.AreEqual(1, patTable.Count);

        }

    }
}
