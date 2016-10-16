using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1
{
    [TestClass]
    public class AbnormalCaseTests
    {
        [TestMethod]
        public void TestHeading1()
        {
            var root = Utility.ParseAndAssert("==", "P[==]");
        }

        [TestMethod]
        public void TestHeading2()
        {
            var root = Utility.ParseAndAssert("===", "H1[=]");
        }

        [TestMethod]
        public void TestHeading3()
        {
            var root = Utility.ParseAndAssert("====", "H1[==]");
        }

        [TestMethod]
        public void TestHeading4()
        {
            var root = Utility.ParseAndAssert("=====", "H2[=]");
        }

        [TestMethod]
        public void TestHeading5()
        {
            var root = Utility.ParseAndAssert("======", "H2[==]");
        }

        [TestMethod]
        public void TestHeading6()
        {
            var root = Utility.ParseAndAssert("======Heading======", "H6[Heading]");
        }

        [TestMethod]
        public void TestHeading7()
        {
            var root = Utility.ParseAndAssert("=============", "H6[=]");
        }

        [TestMethod]
        public void TestHeading8()
        {
            var root = Utility.ParseAndAssert("========Heading========", "H6[==Heading==]");
        }
        
        [TestMethod]
        public void TestHeading9()
        {
            var root = Utility.ParseAndAssert("==Heading== Text", "P[==Heading== Text]");
        }

        [TestMethod]
        public void TestHr1()
        {
            var root = Utility.ParseAndAssert("---", "P[---]");
        }

        [TestMethod]
        public void TestHr2()
        {
            var root = Utility.ParseAndAssert("----", "----[]");
        }

        [TestMethod]
        public void TestHr3()
        {
            var root = Utility.ParseAndAssert("---- ", "----[ ]");
        }


        [TestMethod]
        public void TestHr4()
        {
            var root = Utility.ParseAndAssert("----\n", "----[]P[]");
        }
        
        [TestMethod]
        public void TestHr5()
        {
            var root = Utility.ParseAndAssert("----------", "----------[]");
        }

        [TestMethod]
        public void TestHr6()
        {
            // Stars will be shown as normal text.
            var root = Utility.ParseAndAssert("----****", "----[****]");
        }

        [TestMethod]
        public void TestParagraph1()
        {
            var root = Utility.ParseAndAssert("Line1\n    \t    \n    Line2", "P[Line1\n    \t    ] [   Line2]");
        }

        [TestMethod]
        public void TestWikiLink1()
        {
            var root = Utility.ParseAndAssert("[[Target\nTarget]]", "P[$[$[Target\nTarget$]$]]");
        }

        [TestMethod]
        public void TestWikiLink2()
        {
            var root = Utility.ParseAndAssert("[[Target|Text1|Text2]]", "P[[[Target|Text1|Text2]]]");
        }
    }
}
