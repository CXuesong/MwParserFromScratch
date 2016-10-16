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
            root = Utility.ParseAndAssert("\n", "P[\n]");
            root = Utility.ParseAndAssert("\n\n", "P[]P[]");
            root = Utility.ParseAndAssert("\n\n\n", "P[\n]P[\n]");
        }

        [TestMethod]
        public void TestLines1()
        {
            var root = Utility.ParseAndAssert("Hello, world!", "P[Hello, world!]");
            Assert.IsTrue(((Paragraph)root.Lines.First()).Compact);
        }

        [TestMethod]
        public void TestLines2()
        {
            var root = Utility.ParseAndAssert("Hello, world!\n", "P[Hello, world!\n]");
            Assert.IsFalse(((Paragraph)root.Lines.First()).Compact);
        }

        [TestMethod]
        public void TestLines3()
        {
            var root = Utility.ParseAndAssert("Hello, world!\n\n", "P[Hello, world!]P[]");
            root = Utility.ParseAndAssert("Hello, world!\ntest\n", "P[Hello, world!\ntest\n]");
        }

        [TestMethod]
        public void TestLines4()
        {
            var root = Utility.ParseAndAssert("Hello, world!\n\n\n", "P[Hello, world!\n]P[\n]");
            root = Utility.ParseAndAssert("Hello, world!\n\ntest\n", "P[Hello, world!\n]P[test\n]");
        }

        [TestMethod]
        public void TestLines5()
        {
            var root = Utility.ParseAndAssert("Hello, \n\nworld!", "P[Hello, \n]P[world!]");
        }

        [TestMethod]
        public void TestLines6()
        {
            var root = Utility.ParseAndAssert("P1L1\r\nP1L2\r\n\r\nP2L1\r\n\r\n", "P[P1L1\r\nP1L2\r\n\r]P[P2L1\r]P[\r]");
        }

        [TestMethod]
        public void TestList1()
        {
            var root = Utility.ParseAndAssert("* Item1\n*# Item2\n*#Item3\n**Item4", "*[ Item1]*#[ Item2]*#[Item3]**[Item4]");
        }

        [TestMethod]
        public void TestHeading1()
        {
            var root = Utility.ParseAndAssert("test\n= Title =\n", "P[test]H1[ Title ]P[]");
        }

        [TestMethod]
        public void TestHeading2()
        {
            var root = Utility.ParseAndAssert("test\n\n===== Title ===\n", "P[test\n]H3[== Title ]P[]");
        }

        [TestMethod]
        public void TestFormat1()
        {
            var root = Utility.ParseAndAssert("'''Hello''', ''world''!", "P[[B]Hello[B], [I]world[I]!]");
        }

        [TestMethod]
        public void TestFormat2()
        {
            var root = Utility.ParseAndAssert("'''Hello''',\n''world''!", "P[[B]Hello[B],\n[I]world[I]!]");
        }

        [TestMethod]
        public void TestFormat3()
        {
            var root = Utility.ParseAndAssert("'''Hello''',\n\n''world''!", "P[[B]Hello[B],\n]P[[I]world[I]!]");
        }

        [TestMethod]
        public void TestFormat4()
        {
            var root = Utility.ParseAndAssert("'''''Hello''''', '''''world''. World'''!\n", "P[[BI]Hello[BI], [BI]world[I]. World[B]!\n]");
        }

        [TestMethod]
        public void TestComment()
        {
            var root = Utility.ParseAndAssert("Text<!--Comment\n\nComment-->Text\n", "P[Text<!--Comment\n\nComment-->Text\n]");
        }


        [TestMethod]
        public void TestWikiLink1()
        {
            var root = Utility.ParseAndAssert("[[Duck]]\n", "P[[[Duck]]\n]");
            var link = (WikiLink) root.Lines.First().Inlines.First();
            Assert.AreEqual(link.Target.ToString(), "Duck");
            Assert.AreEqual(link.Text, null);
        }

        [TestMethod]
        public void TestWikiLink2()
        {
            var root = Utility.ParseAndAssert("[[Duck|ducks]]\n", "P[[[Duck|ducks]]\n]");
            var link = (WikiLink) root.Lines.First().Inlines.First();
            Assert.AreEqual(link.Target.ToString(), "Duck");
            Assert.AreEqual(link.Text.ToString(), "ducks");
        }

        [TestMethod]
        public void TestExternalLink1()
        {
            var root = Utility.ParseAndAssert("http://cxuesong.com\n", "P[-[http://cxuesong.com]-\n]");
            var link = (ExternalLink)root.Lines.First().Inlines.First();
            Assert.AreEqual(link.Target.ToString(), "http://cxuesong.com");
            Assert.AreEqual(link.Text, null);
            Assert.IsFalse(link.Brackets);
        }

        [TestMethod]
        public void TestExternalLink2()
        {
            var root = Utility.ParseAndAssert("[http://cxuesong.com]\n", "P[[http://cxuesong.com]\n]");
            var link = (ExternalLink)root.Lines.First().Inlines.First();
            Assert.AreEqual(link.Target.ToString(), "http://cxuesong.com");
            Assert.AreEqual(link.Text, null);
            Assert.IsTrue(link.Brackets);
        }

        [TestMethod]
        public void TestExternalLink3()
        {
            var root = Utility.ParseAndAssert("[https://zh.wikipedia.org   Chinese Wikipedia  ]\n",
                "P[[https://zh.wikipedia.org   Chinese Wikipedia  ]\n]");
            var link = (ExternalLink) root.Lines.First().Inlines.First();
            Assert.AreEqual(link.Target.ToString(), "https://zh.wikipedia.org");
            Assert.AreEqual(link.Text.ToString(), "  Chinese Wikipedia  ");
            Assert.IsTrue(link.Brackets);
        }
    }
}
