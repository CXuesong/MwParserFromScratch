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
            root = Utility.ParseAndAssert("\n\n", "P[]\nP[]");
            root = Utility.ParseAndAssert("\n\n\n", "P[\n]\nP[\n]");
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
            var root = Utility.ParseAndAssert("Hello, world!\n\n", "P[Hello, world!]\nP[]");
            root = Utility.ParseAndAssert("Hello, world!\ntest\n", "P[Hello, world!\ntest\n]");
        }

        [TestMethod]
        public void TestLines4()
        {
            var root = Utility.ParseAndAssert("Hello, world!\n\n\n", "P[Hello, world!\n]\nP[\n]");
            root = Utility.ParseAndAssert("Hello, world!\n\ntest\n", "P[Hello, world!\n]\nP[test\n]");
        }

        [TestMethod]
        public void TestLines5()
        {
            var root = Utility.ParseAndAssert("Hello, \n\nworld!", "P[Hello, \n]\nP[world!]");
        }

        [TestMethod]
        public void TestLines6()
        {
            var root = Utility.ParseAndAssert("P1L1\r\nP1L2\r\n\r\nP2L1\r\n\r\n", "P[P1L1\r\nP1L2\r\n\r]\nP[P2L1\r]\nP[\r]");
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
            var root = Utility.ParseAndAssert("'''Hello''',\n\n''world''!", "P[[B]Hello[B],\n]\nP[[I]world[I]!]");
        }

        [TestMethod]
        public void TestFormat4()
        {
            var root = Utility.ParseAndAssert("'''''Hello''''', '''''world''. World'''!\n", "P[[BI]Hello[BI], [BI]world[I]. World[B]!\n]");
        }

        [TestMethod]
        public void TestComment()
        {
            var root = Utility.ParseAndAssert("Text<!--Comment\n\nComment-->Text\n", "P[Text![Comment\n\nComment]Text\n]");
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
            var root = Utility.ParseAndAssert("http://cxuesong.com\n", "P[[http://cxuesong.com]\n]");
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
