using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;

namespace UnitTestProject1
{
    [TestClass]
    public class BasicParsingTests
    {
        [TestMethod]
        public void TestMethod0()
        {
            var root = Utility.ParseAndAssert("", "");
            root = Utility.ParseAndAssert("\n", "P[||]");
            Assert.AreEqual(1, root.Lines.Count());
            Assert.IsTrue(((Paragraph) root.Lines.First()).Compact);
            root = Utility.ParseAndAssert("\n\n", "P[||]");
            Assert.AreEqual(1, root.Lines.Count());
            Assert.IsFalse(((Paragraph) root.Lines.First()).Compact);
            root = Utility.ParseAndAssert("\n\n\n", "P[||]P[||]");
            Assert.AreEqual(2, root.Lines.Count());
            Assert.IsFalse(((Paragraph) root.Lines.ElementAt(0)).Compact);
            Assert.IsTrue(((Paragraph) root.Lines.ElementAt(1)).Compact);
        }

        [TestMethod]
        public void TestMethod1()
        {
            var root = Utility.ParseAndAssert("Hello, world!", "P[|Hello, world!|]");
        }

        [TestMethod]
        public void TestMethod2()
        {
            var root = Utility.ParseAndAssert("Hello, \n\nworld!", "P[|Hello, |]\nP[|world!|]");
        }

        [TestMethod]
        public void TestMethod3()
        {
            var root = Utility.ParseAndAssert("'''Hello''', ''world''!", "P[|Hello, world!|]");
        }

        [TestMethod]
        public void TestMethod4()
        {
            var root = Utility.ParseAndAssert("'''Hello''',\n''world''!", "P[|Hello, world!|]");
        }

        [TestMethod]
        public void TestMethod5()
        {
            var root = Utility.ParseAndAssert("'''Hello''',\n\n''world''!", "P[|Hello, world!|]");
        }

        [TestMethod]
        public void TestMethod6()
        {
            var root = Utility.ParseAndAssert("'''Hello''',\n\n''world''!\n", "P[|Hello, world!|]");
        }
    }
}
