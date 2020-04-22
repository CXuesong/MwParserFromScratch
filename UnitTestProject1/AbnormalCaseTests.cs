using System;
using Xunit;
using Xunit.Sdk;

namespace UnitTestProject1
{

    public class AbnormalCaseTests
    {
        [Fact]
        public void TestHeading1()
        {
            var root = Utility.ParseAndAssert("==", "P[==]");
        }

        [Fact]
        public void TestHeading2()
        {
            var root = Utility.ParseAndAssert("===", "H1[=]");
        }

        [Fact]
        public void TestHeading3()
        {
            var root = Utility.ParseAndAssert("====", "H1[==]");
        }

        [Fact]
        public void TestHeading4()
        {
            var root = Utility.ParseAndAssert("=====", "H2[=]");
        }

        [Fact]
        public void TestHeading5()
        {
            var root = Utility.ParseAndAssert("======", "H2[==]");
        }

        [Fact]
        public void TestHeading6()
        {
            var root = Utility.ParseAndAssert("======Heading======", "H6[Heading]");
        }

        [Fact]
        public void TestHeading7()
        {
            var root = Utility.ParseAndAssert("=============", "H6[=]");
        }

        [Fact]
        public void TestHeading8()
        {
            var root = Utility.ParseAndAssert("========Heading========", "H6[==Heading==]");
        }
        
        [Fact]
        public void TestHeading9()
        {
            var root = Utility.ParseAndAssert("==Heading== Text", "P[==Heading== Text]");
        }

        [Fact]
        public void TestHeading10()
        {
            var root = Utility.ParseAndAssert("==A=<!--abc-->", "H1[=A][<!--abc-->]");
        }

        [Fact]
        public void TestHr1()
        {
            var root = Utility.ParseAndAssert("---", "P[---]");
        }

        [Fact]
        public void TestHr2()
        {
            var root = Utility.ParseAndAssert("----", "----[]");
        }

        [Fact]
        public void TestHr3()
        {
            var root = Utility.ParseAndAssert("---- ", "----[ ]");
        }


        [Fact]
        public void TestHr4()
        {
            var root = Utility.ParseAndAssert("----\n", "----[]P[]");
        }
        
        [Fact]
        public void TestHr5()
        {
            var root = Utility.ParseAndAssert("----------", "----------[]");
        }

        [Fact]
        public void TestHr6()
        {
            // Stars will be shown as normal text.
            var root = Utility.ParseAndAssert("----****", "----[****]");
        }

        [Fact]
        public void TestParagraph1()
        {
            var root = Utility.ParseAndAssert("Line1\n    \t    \n    Line2", "P[Line1\n    \t    ] [   Line2]");
        }

        [Fact]
        public void TestWikiLink1()
        {
            var root = Utility.ParseAndAssert("[[Target\nTarget]]", "P[$[$[Target\nTarget$]$]]");
        }

        [Fact]
        public void TestWikiLink2()
        {
            var root = Utility.ParseAndAssert("[[Target|Text1|Text2]]", "P[[[Target|Text1|Text2]]]");
        }

        [Fact]
        public void TestWikiLink3()
        {
            var root = Utility.ParseAndAssert("[[Target[[Text1|Text2]]", "P[$[$[Target[[Text1|Text2]]]");
        }

        [Fact]
        public void TestOverlap1()
        {
            var root = Utility.ParseAndAssert("[[Target/{{T|]]=abc}}|Title]]", "P[[[Target/{{T|P[$]$]]=P[abc]}}|Title]]]");
        }

        [Fact]
        public void TestOverlap2()
        {
            var root = Utility.ParseAndAssert("{{T|a=[[Link}}text]]", "P[${${T|a=[[Link$}$}text]]]");
        }

        [Fact]
        public void TestBraces1()
        {
            var root = Utility.ParseAndAssert("{{{{arg}}}}", "P[${{{{P[arg]}}}$}]");
        }

        [Fact]
        public void TestBraces2()
        {
            var root = Utility.ParseAndAssert("{{{{{arg}}}|test}}", "P[{{{{{arg}}}|P[test]}}]");
        }

        [Fact]
        public void TestBraces3()
        {
            // This hasn't been solved yet.
            Assert.Throws<EqualException>(() => Utility.ParseAndAssert("{{{{{arg}}", "P[${${${{{arg}}]"));
        }

        [Fact]
        public void TestBraces4()
        {
            var root = Utility.ParseAndAssert("{{{{{arg", "P[${${${${${arg]");
        }

        [Fact]
        public void TestBraces5()
        {
            var root = Utility.ParseAndAssert("{arg", "P[${arg]");
        }


        [Fact]
        public void TestBraces6()
        {
            var root = Utility.ParseAndAssert("{{arg", "P[${${arg]");
        }
        
        [Fact]
        public void TestBraces7()
        {
            var root = Utility.ParseAndAssert("{{{arg", "P[${${${arg]");
        }

        [Fact]
        public void TestBraces8()
        {
            var root = Utility.ParseAndAssert("{{{arg}}}}}", "P[{{{P[arg]}}}$}$}]");
        }

        [Fact]
        public void TestTag1()
        {
            var root = Utility.ParseAndAssert("abc<br>def", "P[abc<br>def]");
            root = Utility.ParseAndAssert("abc<bR>def", "P[abc<bR>def]");
            root = Utility.ParseAndAssert("abc<br />def", "P[abc<br />def]");
        }

        [Fact]
        public void TestTag2()
        {
            var root = Utility.ParseAndAssert("abc<div>def", "P[abc$<div$>def]");
            root = Utility.ParseAndAssert("abc<div />def", "P[abc<div />def]");
        }

    }
}
