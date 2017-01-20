using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patternson
{
    [TestClass]
    public class PatternRecognitionTest
    {

        [TestMethod]
        public void SearchPatternTest()
        {
            var testPatRecog = new PatternRecognition();

            var testString = "A.B.DC..AA.BCDA.BCDAA.B..C";

            var testData = new List<byte>();

            foreach (char c in testString.ToCharArray())
                testData.Add((byte)(c));

            var testPatTable = testPatRecog.SearchPattern(testData.ToArray<byte>());

            var testResult = testPatTable.AsText();

            Assert.IsTrue(testResult.Contains("(0)A (11)A\nt: 8 9\n"));
            Assert.IsTrue(testResult.Contains("(0)A (2)B (3)C (4)D (5)A\nt: 9 14\n"));
            Assert.IsTrue(testResult.Contains("(0)A (6)A (8)B\nt: 8 14\n"));
            Assert.IsTrue(testResult.Contains("(0)A (17)C\nt: 0 8\n"));
            Assert.IsTrue(testResult.Contains("(0)A (2)B (4)D\nt: 0 9 14\n"));
            Assert.IsTrue(testResult.Contains("(0)A (1)A (3)B\nt: 8 19\n"));
            Assert.IsTrue(testResult.Contains("(0)C (3)A\nt: 5 17\n"));
            Assert.IsTrue(testResult.Contains("(0)A (2)B (5)C\nt: 0 20"));
            
        }

    }
}
