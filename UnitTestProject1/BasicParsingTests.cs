using System;
using System.Diagnostics;
using System.Linq;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using UnitTestProject1.Primitive;
using Xunit;
using Xunit.Abstractions;

namespace UnitTestProject1
{

    public class BasicParsingTests : ParserTestBase
    {

        /// <inheritdoc />
        public BasicParsingTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void TestLines0()
        {
            var root = ParseAndAssert("", "");
            root = ParseAndAssert("\n", "P[\n]");
            root = ParseAndAssert("\n\n", "P[\n]P[]");
            root = ParseAndAssert("\n\n\n", "P[\n]P[\n]");
        }

        [Fact]
        public void TestLines1()
        {
            var root = ParseAndAssert("Hello, world!", "P[Hello, world!]");
            Assert.True(((Paragraph)root.Lines.First()).Compact);
        }

        [Fact]
        public void TestLines2()
        {
            var root = ParseAndAssert("Hello, world!\n", "P[Hello, world!\n]");
            Assert.False(((Paragraph)root.Lines.First()).Compact);
        }

        [Fact]
        public void TestLines3()
        {
            var root = ParseAndAssert("Hello, world!\n\n", "P[Hello, world!\n]P[]");
            root = ParseAndAssert("Hello, world!\ntest\n", "P[Hello, world!\ntest\n]");
        }

        [Fact]
        public void TestLines4()
        {
            var root = ParseAndAssert("Hello, world!\n\n\n", "P[Hello, world!\n]P[\n]");
            root = ParseAndAssert("Hello, world!\n\ntest\n", "P[Hello, world!\n]P[test\n]");
        }

        [Fact]
        public void TestLines5()
        {
            var root = ParseAndAssert("Hello, \n\nworld!", "P[Hello, \n]P[world!]");
        }

        [Fact]
        public void TestLines6()
        {
            var root = ParseAndAssert("P1L1\r\nP1L2\r\n\r\nP2L1\r\n\r\n", "P[P1L1\r\nP1L2\r\n\r]P[P2L1\r\n\r]P[]");
        }

        [Fact]
        public void TestList1()
        {
            var root = ParseAndAssert("* Item1\n*# Item2\n*#Item3\n**Item4", "*[ Item1]*#[ Item2]*#[Item3]**[Item4]");
        }

        [Fact]
        public void TestList2()
        {
            var root = ParseAndAssert("* Item1\n*# Item2\n    Item3\n**Item4", "*[ Item1]*#[ Item2] [   Item3]**[Item4]");
        }

        [Fact]
        public void TestList3()
        {
            // Mixed
            var root = ParseAndAssert("* Item1\nLine2\n  Item3\nLine4\n", "*[ Item1]P[Line2] [ Item3]P[Line4\n]");
        }

        [Fact]
        public void TestHeading1()
        {
            var root = ParseAndAssert("test\n= Title =\n", "P[test]H1[ Title ]P[]");
        }

        [Fact]
        public void TestHeading2()
        {
            var root = ParseAndAssert("test\n\n===== Title ===\n", "P[test\n]H3[== Title ]P[]");
        }

        [Fact]
        public void TestFormat1()
        {
            var root = ParseAndAssert("'''Hello''', ''world''!", "P[[B]Hello[B], [I]world[I]!]");
        }

        [Fact]
        public void TestFormat2()
        {
            var root = ParseAndAssert("'''Hello''',\n''world''!", "P[[B]Hello[B],\n[I]world[I]!]");
        }

        [Fact]
        public void TestFormat3()
        {
            var root = ParseAndAssert("'''Hello''',\n\n''world''!", "P[[B]Hello[B],\n]P[[I]world[I]!]");
        }

        [Fact]
        public void TestFormat4()
        {
            var root = ParseAndAssert("'''''Hello''''', '''''world''. World'''!\n", "P[[BI]Hello[BI], [BI]world[I]. World[B]!\n]");
        }

        [Fact]
        public void TestComment()
        {
            var root = ParseAndAssert("Text<!--Comment\n\nComment-->Text\n", "P[Text<!--Comment\n\nComment-->Text\n]");
        }


        [Fact]
        public void TestWikiLink1()
        {
            var root = ParseAndAssert("[[Duck]]\n", "P[[[Duck]]\n]");
            var link = (WikiLink) root.Lines.First().EnumChildren().First();
            Assert.Equal("Duck", link.Target.ToString());
            Assert.Null(link.Text);
        }

        [Fact]
        public void TestWikiLink2()
        {
            var root = ParseAndAssert("[[Duck|ducks]]\n", "P[[[Duck|ducks]]\n]");
            var link = (WikiLink) root.Lines.First().EnumChildren().First();
            Assert.Equal("Duck", link.Target.ToString());
            Assert.Equal("ducks", link.Text.ToString());
        }

        [Fact]
        public void TestWikiImageLink1()
        {
            var root = ParseAndAssert(
                "[[File:example.jpg|link=http://wikipedia.org/wiki/Test|thumb|upright|caption|caption2]]",
                "P[![[File:example.jpg|P[link]=P[-[http://wikipedia.org/wiki/Test]-]|P[thumb]|P[upright]|P[caption]|P[caption2]]]]");
            Assert.Equal("caption2", root.EnumDescendants().OfType<WikiImageLink>().First().Arguments.Caption.ToString());
            Assert.Equal("http://wikipedia.org/wiki/Test", root.EnumDescendants().OfType<WikiImageLink>().First().Arguments.Link.ToString());
            root = ParseAndAssert(
                "[[Bestand:Bundesarchiv Bild 146III-373, Modell der Neugestaltung Berlins (\"Germania\").jpg|miniatuur|260px|right| Schaalmodel van de [[Welthauptstadt Germania]], 1939]]",
                "P[![[Bestand:Bundesarchiv Bild 146III-373, Modell der Neugestaltung Berlins (\"Germania\").jpg|P[miniatuur]|P[260px]|P[right]|P[ Schaalmodel van de [[Welthauptstadt Germania]], 1939]]]]",
                new WikitextParserOptions { ImageNamespaceNames = new[] { "File", "Image", "bestand" } });
            Assert.Equal(" Schaalmodel van de Welthauptstadt Germania, 1939", root.ToPlainText());
        }

        [Fact]
        public void TestWikiImageLink2()
        {
            // Cannot contain \n in TARGET
            ParseAndAssert(
                "[[File:target.png\ntest.png]]",
                "P[$[$[File:target.png\ntest.png$]$]]");
            // Can contain \n in arguments
            ParseAndAssert(
                "[[File:target.png|caption 1\ncaption 2]]",
                "P[![[File:target.png|P[caption 1\ncaption 2]]]]");
        }

        [Fact]
        public void TestExternalLink1()
        {
            var root = ParseAndAssert("http://cxuesong.com\n", "P[-[http://cxuesong.com]-\n]");
            var link = (ExternalLink)root.Lines.First().EnumChildren().First();
            Assert.Equal("http://cxuesong.com", link.Target.ToString());
            Assert.Null(link.Text);
            Assert.False(link.Brackets);
        }

        [Fact]
        public void TestExternalLink2()
        {
            var root = ParseAndAssert("[http://cxuesong.com]\n", "P[[http://cxuesong.com]\n]");
            var link = (ExternalLink)root.Lines.First().EnumChildren().First();
            Assert.Equal("http://cxuesong.com", link.Target.ToString());
            Assert.Null(link.Text);
            Assert.True(link.Brackets);
        }

        [Fact]
        public void TestExternalLink3()
        {
            var root = ParseAndAssert("[https://zh.wikipedia.org   Chinese Wikipedia  ]\n",
                "P[[https://zh.wikipedia.org   Chinese Wikipedia  ]\n]");
            var link = (ExternalLink) root.Lines.First().EnumChildren().First();
            Assert.Equal("https://zh.wikipedia.org", link.Target.ToString());
            Assert.Equal("  Chinese Wikipedia  ", link.Text.ToString());
            Assert.True(link.Brackets);
        }
    }
}
