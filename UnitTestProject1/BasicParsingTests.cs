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

        [TestMethod]
        public void TestComment()
        {
            var root = Utility.ParseAndAssert("Text<!--Comment\n\nComment-->Text\n", "P[|TextC[|Comment\n\nComment|]Text|]");
        }


        [TestMethod]
        public void TestWikiLink1()
        {
            var root = Utility.ParseAndAssert("[[Duck]]\n", "P[|[[Duck]]|]");
            var link = (WikiLink) root.Lines.First().Inlines.First();
            Assert.AreEqual(link.Target.ToString(), "Duck");
            Assert.AreEqual(link.Text, null);
        }

        [TestMethod]
        public void TestWikiLink2()
        {
            var root = Utility.ParseAndAssert("[[Duck|ducks]]\n", "P[|[[Duck|ducks]]|]");
            var link = (WikiLink) root.Lines.First().Inlines.First();
            Assert.AreEqual(link.Target.ToString(), "Duck");
            Assert.AreEqual(link.Text.ToString(), "ducks");
        }

        [TestMethod]
        public void TestExternalLink1()
        {
            var root = Utility.ParseAndAssert("http://cxuesong.com\n", "P[|[http://cxuesong.com]|]");
            var link = (ExternalLink)root.Lines.First().Inlines.First();
            Assert.AreEqual(link.Target.ToString(), "http://cxuesong.com");
            Assert.AreEqual(link.Text, null);
            Assert.IsFalse(link.Brackets);
        }

        [TestMethod]
        public void TestExternalLink2()
        {
            var root = Utility.ParseAndAssert("[http://cxuesong.com]\n", "P[|[http://cxuesong.com]|]");
            var link = (ExternalLink)root.Lines.First().Inlines.First();
            Assert.AreEqual(link.Target.ToString(), "http://cxuesong.com");
            Assert.AreEqual(link.Text, null);
            Assert.IsTrue(link.Brackets);
        }

        [TestMethod]
        public void TestExternalLink3()
        {
            var root = Utility.ParseAndAssert("[https://zh.wikipedia.org   Chinese Wikipedia  ]\n",
                "P[|[https://zh.wikipedia.org   Chinese Wikipedia  ]|]");
            var link = (ExternalLink) root.Lines.First().Inlines.First();
            Assert.AreEqual(link.Target.ToString(), "https://zh.wikipedia.org");
            Assert.AreEqual(link.Text.ToString(), "  Chinese Wikipedia  ");
            Assert.IsTrue(link.Brackets);
        }
    }
}
