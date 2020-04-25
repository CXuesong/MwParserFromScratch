using System;
using System.Linq;
using MwParserFromScratch.Nodes;
using UnitTestProject1.Primitive;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace UnitTestProject1
{

    public class AbnormalCaseTests : ParserTestBase
    {

        /// <inheritdoc />
        public AbnormalCaseTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void TestHeading1()
        {
            var root = ParseAndAssert("==", "P[==]");
        }

        [Fact]
        public void TestHeading2()
        {
            var root = ParseAndAssert("===", "H1[=]");
        }

        [Fact]
        public void TestHeading3()
        {
            var root = ParseAndAssert("====", "H1[==]");
        }

        [Fact]
        public void TestHeading4()
        {
            var root = ParseAndAssert("=====", "H2[=]");
        }

        [Fact]
        public void TestHeading5()
        {
            var root = ParseAndAssert("======", "H2[==]");
        }

        [Fact]
        public void TestHeading6()
        {
            var root = ParseAndAssert("======Heading======", "H6[Heading]");
        }

        [Fact]
        public void TestHeading7()
        {
            var root = ParseAndAssert("=============", "H6[=]");
        }

        [Fact]
        public void TestHeading8()
        {
            var root = ParseAndAssert("========Heading========", "H6[==Heading==]");
        }
        
        [Fact]
        public void TestHeading9()
        {
            var root = ParseAndAssert("==Heading== Text", "P[==Heading== Text]");
        }

        [Fact]
        public void TestHeading10()
        {
            var root = ParseAndAssert("==A=<!--abc-->", "H1[=A][<!--abc-->]");
        }

        [Fact]
        public void TestHr1()
        {
            var root = ParseAndAssert("---", "P[---]");
        }

        [Fact]
        public void TestHr2()
        {
            var root = ParseAndAssert("----", "----[]");
        }

        [Fact]
        public void TestHr3()
        {
            var root = ParseAndAssert("---- ", "----[ ]");
        }


        [Fact]
        public void TestHr4()
        {
            var root = ParseAndAssert("----\n", "----[]P[]");
        }
        
        [Fact]
        public void TestHr5()
        {
            var root = ParseAndAssert("----------", "----------[]");
        }

        [Fact]
        public void TestHr6()
        {
            // Stars will be shown as normal text.
            var root = ParseAndAssert("----****", "----[****]");
        }

        [Fact]
        public void TestParagraph1()
        {
            var root = ParseAndAssert("Line1\n    \t    \n    Line2", "P[Line1\n    \t    ] [   Line2]");
        }

        [Fact]
        public void TestWikiLink1()
        {
            var root = ParseAndAssert("[[Target\nTarget]]", "P[$[$[Target\nTarget$]$]]");
        }

        [Fact]
        public void TestWikiLink2()
        {
            var root = ParseAndAssert("[[Target|Text1|Text2]]", "P[[[Target|Text1|Text2]]]");
        }

        [Fact]
        public void TestWikiLink3()
        {
            var root = ParseAndAssert("[[Target[[Text1|Text2]]", "P[$[$[Target[[Text1|Text2]]]");
        }

        [Fact]
        public void TestOverlap1()
        {
            var root = ParseAndAssert("[[Target/{{T|]]=abc}}|Title]]", "P[[[Target/{{T|P[$]$]]=P[abc]}}|Title]]]");
        }

        [Fact]
        public void TestOverlap2()
        {
            var root = ParseAndAssert("{{T|a=[[Link}}text]]", "P[${${T|a=[[Link$}$}text]]]");
        }

        [Fact]
        public void TestBraces1()
        {
            var root = ParseAndAssert("{{{{arg}}}}", "P[${{{{P[arg]}}}$}]");
        }

        [Fact]
        public void TestBraces2()
        {
            var root = ParseAndAssert("{{{{{arg}}}|test}}", "P[{{{{{arg}}}|P[test]}}]");
        }

        [Fact]
        public void TestBraces3()
        {
            // This hasn't been solved yet.
            Assert.Throws<EqualException>(() => ParseAndAssert("{{{{{arg}}", "P[${${${{{arg}}]"));
        }

        [Fact]
        public void TestBraces4()
        {
            var root = ParseAndAssert("{{{{{arg", "P[${${${${${arg]");
        }

        [Fact]
        public void TestBraces5()
        {
            var root = ParseAndAssert("{arg", "P[${arg]");
        }


        [Fact]
        public void TestBraces6()
        {
            var root = ParseAndAssert("{{arg", "P[${${arg]");
        }
        
        [Fact]
        public void TestBraces7()
        {
            var root = ParseAndAssert("{{{arg", "P[${${${arg]");
        }

        [Fact]
        public void TestBraces8()
        {
            var root = ParseAndAssert("{{{arg}}}}}", "P[{{{P[arg]}}}$}$}]");
        }

        [Fact]
        public void TestTag1()
        {
            var root = ParseAndAssert("abc<br>def", "P[abc<br>def]");
            root = ParseAndAssert("abc<bR>def", "P[abc<bR>def]");
            root = ParseAndAssert("abc<br />def", "P[abc<br />def]");
        }

        [Fact]
        public void TestTag2()
        {
            // Unbalanced tags.
            var root = ParseAndAssert("abc<div>def", "P[abc<div>P[def]</div>]");
            Assert.Equal(TagStyle.NotClosed, root.EnumDescendants().OfType<HtmlTag>().First().TagStyle);
            Assert.Null(root.EnumDescendants().OfType<HtmlTag>().First().ClosingTagName);
            ParseAndAssert("abc<div>def<div>ghi", "P[abc<div>P[def<div>P[ghi]</div>]</div>]");
            // <div> tag is forced to close at the end of {{T}}
            ParseAndAssert("{{T|abc<div>def}}</div>ghi", "P[{{T|P[abc<div>P[def]</div>]}}$</div$>ghi]");
            ParseAndAssert("abc<div />def", "P[abc<div />def]");
        }

        [Fact]
        public void TestTag3()
        {
            // Unbalanced li tags.
            ParseAndAssert("header<li>item 1", "P[header<li>P[item 1]</li>]");
            ParseAndAssert("header<li>item 1\nfooter", "P[header<li>P[item 1\nfooter]</li>]");
            ParseAndAssert("header<li>item 1<li>item 2", "P[header<li>P[item 1]</li><li>P[item 2]</li>]");
            ParseAndAssert("header<li>item 1<li >item 2", "P[header<li>P[item 1]</li><li >P[item 2]</li>]");
            var root = ParseAndAssert("header<li>item 1<li>item 2<li value='100' >item 100\nfooter", "P[header<li>P[item 1]</li><li>P[item 2]</li><li value='100' >P[item 100\nfooter]</li>]");
            Assert.All(root.EnumDescendants().OfType<HtmlTag>(), t => Assert.Equal(TagStyle.NotClosed, t.TagStyle));
            root = ParseAndAssert("header<li>item 1<li>item 2<li>item 3\nfooter", "P[header<li>P[item 1]</li><li>P[item 2]</li><li>P[item 3\nfooter]</li>]");
            Assert.All(root.EnumDescendants().OfType<HtmlTag>(), t => Assert.Equal(TagStyle.NotClosed, t.TagStyle));
        }

    }
}
