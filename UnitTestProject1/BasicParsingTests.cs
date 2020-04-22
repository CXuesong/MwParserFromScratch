using System;
using System.Diagnostics;
using System.Linq;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using Xunit;

namespace UnitTestProject1
{

    public class BasicParsingTests
    {
        [Fact]
        public void TestLines0()
        {
            var root = Utility.ParseAndAssert("", "");
            root = Utility.ParseAndAssert("\n", "P[\n]");
            root = Utility.ParseAndAssert("\n\n", "P[\n]P[]");
            root = Utility.ParseAndAssert("\n\n\n", "P[\n]P[\n]");
        }

        [Fact]
        public void TestLines1()
        {
            var root = Utility.ParseAndAssert("Hello, world!", "P[Hello, world!]");
            Assert.True(((Paragraph)root.Lines.First()).Compact);
        }

        [Fact]
        public void TestLines2()
        {
            var root = Utility.ParseAndAssert("Hello, world!\n", "P[Hello, world!\n]");
            Assert.False(((Paragraph)root.Lines.First()).Compact);
        }

        [Fact]
        public void TestLines3()
        {
            var root = Utility.ParseAndAssert("Hello, world!\n\n", "P[Hello, world!\n]P[]");
            root = Utility.ParseAndAssert("Hello, world!\ntest\n", "P[Hello, world!\ntest\n]");
        }

        [Fact]
        public void TestLines4()
        {
            var root = Utility.ParseAndAssert("Hello, world!\n\n\n", "P[Hello, world!\n]P[\n]");
            root = Utility.ParseAndAssert("Hello, world!\n\ntest\n", "P[Hello, world!\n]P[test\n]");
        }

        [Fact]
        public void TestLines5()
        {
            var root = Utility.ParseAndAssert("Hello, \n\nworld!", "P[Hello, \n]P[world!]");
        }

        [Fact]
        public void TestLines6()
        {
            var root = Utility.ParseAndAssert("P1L1\r\nP1L2\r\n\r\nP2L1\r\n\r\n", "P[P1L1\r\nP1L2\r\n\r]P[P2L1\r\n\r]P[]");
        }

        [Fact]
        public void TestList1()
        {
            var root = Utility.ParseAndAssert("* Item1\n*# Item2\n*#Item3\n**Item4", "*[ Item1]*#[ Item2]*#[Item3]**[Item4]");
        }

        [Fact]
        public void TestList2()
        {
            var root = Utility.ParseAndAssert("* Item1\n*# Item2\n    Item3\n**Item4", "*[ Item1]*#[ Item2] [   Item3]**[Item4]");
        }

        [Fact]
        public void TestList3()
        {
            // Mixed
            var root = Utility.ParseAndAssert("* Item1\nLine2\n  Item3\nLine4\n", "*[ Item1]P[Line2] [ Item3]P[Line4\n]");
        }

        [Fact]
        public void TestHeading1()
        {
            var root = Utility.ParseAndAssert("test\n= Title =\n", "P[test]H1[ Title ]P[]");
        }

        [Fact]
        public void TestHeading2()
        {
            var root = Utility.ParseAndAssert("test\n\n===== Title ===\n", "P[test\n]H3[== Title ]P[]");
        }

        [Fact]
        public void TestFormat1()
        {
            var root = Utility.ParseAndAssert("'''Hello''', ''world''!", "P[[B]Hello[B], [I]world[I]!]");
        }

        [Fact]
        public void TestFormat2()
        {
            var root = Utility.ParseAndAssert("'''Hello''',\n''world''!", "P[[B]Hello[B],\n[I]world[I]!]");
        }

        [Fact]
        public void TestFormat3()
        {
            var root = Utility.ParseAndAssert("'''Hello''',\n\n''world''!", "P[[B]Hello[B],\n]P[[I]world[I]!]");
        }

        [Fact]
        public void TestFormat4()
        {
            var root = Utility.ParseAndAssert("'''''Hello''''', '''''world''. World'''!\n", "P[[BI]Hello[BI], [BI]world[I]. World[B]!\n]");
        }

        [Fact]
        public void TestComment()
        {
            var root = Utility.ParseAndAssert("Text<!--Comment\n\nComment-->Text\n", "P[Text<!--Comment\n\nComment-->Text\n]");
        }


        [Fact]
        public void TestWikiLink1()
        {
            var root = Utility.ParseAndAssert("[[Duck]]\n", "P[[[Duck]]\n]");
            var link = (WikiLink) root.Lines.First().EnumChildren().First();
            Assert.Equal("Duck", link.Target.ToString());
            Assert.Null(link.Text);
        }

        [Fact]
        public void TestWikiLink2()
        {
            var root = Utility.ParseAndAssert("[[Duck|ducks]]\n", "P[[[Duck|ducks]]\n]");
            var link = (WikiLink) root.Lines.First().EnumChildren().First();
            Assert.Equal("Duck", link.Target.ToString());
            Assert.Equal("ducks", link.Text.ToString());
        }

        [Fact]
        public void TestExternalLink1()
        {
            var root = Utility.ParseAndAssert("http://cxuesong.com\n", "P[-[http://cxuesong.com]-\n]");
            var link = (ExternalLink)root.Lines.First().EnumChildren().First();
            Assert.Equal("http://cxuesong.com", link.Target.ToString());
            Assert.Null(link.Text);
            Assert.False(link.Brackets);
        }

        [Fact]
        public void TestExternalLink2()
        {
            var root = Utility.ParseAndAssert("[http://cxuesong.com]\n", "P[[http://cxuesong.com]\n]");
            var link = (ExternalLink)root.Lines.First().EnumChildren().First();
            Assert.Equal("http://cxuesong.com", link.Target.ToString());
            Assert.Null(link.Text);
            Assert.True(link.Brackets);
        }

        [Fact]
        public void TestExternalLink3()
        {
            var root = Utility.ParseAndAssert("[https://zh.wikipedia.org   Chinese Wikipedia  ]\n",
                "P[[https://zh.wikipedia.org   Chinese Wikipedia  ]\n]");
            var link = (ExternalLink) root.Lines.First().EnumChildren().First();
            Assert.Equal("https://zh.wikipedia.org", link.Target.ToString());
            Assert.Equal("  Chinese Wikipedia  ", link.Text.ToString());
            Assert.True(link.Brackets);
        }
    }
}
