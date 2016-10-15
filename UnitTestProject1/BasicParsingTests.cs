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
        public void TestLines0()
        {
            var root = Utility.ParseAndAssert("", "");
            root = Utility.ParseAndAssert("\n", "P[||]");
            root = Utility.ParseAndAssert("\n\n", "PC[||]\nPC[||]");
            root = Utility.ParseAndAssert("\n\n\n", "P[||]\nP[||]");
        }

        [TestMethod]
        public void TestLines1()
        {
            var root = Utility.ParseAndAssert("Hello, world!", "PC[|Hello, world!|]");
            Assert.IsTrue(((Paragraph)root.Lines.First()).Compact);
        }

        [TestMethod]
        public void TestLines2()
        {
            var root = Utility.ParseAndAssert("Hello, world!\n", "P[|Hello, world!|]");
            Assert.IsFalse(((Paragraph)root.Lines.First()).Compact);
        }

        [TestMethod]
        public void TestLines3()
        {
            var root = Utility.ParseAndAssert("Hello, world!\n\n", "PC[|Hello, world!|]\nPC[||]");
            root = Utility.ParseAndAssert("Hello, world!\ntest\n", "P[|Hello, world!\ntest|]");
        }

        [TestMethod]
        public void TestLines4()
        {
            var root = Utility.ParseAndAssert("Hello, world!\n\n\n", "P[|Hello, world!|]\nP[||]");
            root = Utility.ParseAndAssert("Hello, world!\n\ntest\n", "P[|Hello, world!|]\nP[|test|]");
        }

        [TestMethod]
        public void TestLines5()
        {
            var root = Utility.ParseAndAssert("Hello, \n\nworld!", "P[|Hello, |]\nPC[|world!|]");
        }

        [TestMethod]
        public void TestFormat1()
        {
            var root = Utility.ParseAndAssert("'''Hello''', ''world''!", "PC[|[B]Hello[B], [I]world[I]!|]");
        }

        [TestMethod]
        public void TestFormat2()
        {
            var root = Utility.ParseAndAssert("'''Hello''',\n''world''!", "PC[|[B]Hello[B],\n[I]world[I]!|]");
        }

        [TestMethod]
        public void TestFormat3()
        {
            var root = Utility.ParseAndAssert("'''Hello''',\n\n''world''!", "P[|[B]Hello[B],|]\nPC[|[I]world[I]!|]");
        }

        [TestMethod]
        public void TestFormat4()
        {
            var root = Utility.ParseAndAssert("'''''Hello''''', '''''world''. World'''!\n", "P[|[BI]Hello[BI], [BI]world[I]. World[B]!|]");
        }
    }
}
