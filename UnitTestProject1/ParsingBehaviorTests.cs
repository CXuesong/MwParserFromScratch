using System;
using System.Linq;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using Xunit;

namespace UnitTestProject1
{
    public class ParsingBehaviorTests
    {
        [Fact]
        public void TestMethod1()
        {
            Utility.ParseAndAssert("{{Test}}{{}}[[]][]", "P[{{Test}}${${$}$}$[$[$]$]$[$]]");
            Utility.ParseAndAssert("{{Test}}{{}}[[]][]", "P[{{Test}}{{}}[[]][]]", new WikitextParserOptions
            {
                AllowEmptyTemplateName = true,
                AllowEmptyWikiLinkTarget = true,
                AllowEmptyExternalLinkTarget = true
            });
        }

        [Fact]
        public void TestMethod2()
        {
            var options = new WikitextParserOptions
            {
                AllowClosingMarkInference = true
            };
            var root = Utility.ParseAndAssert("{{Test{{test|a|b|c}}|def|g=h",
                "P[{{Test{{test|a|b|c}}|P[def]|P[g]=P[h]}}]",
                options);
            Assert.True(((IWikitextParsingInfo) root.Lines.FirstNode.EnumChildren().First()).InferredClosingMark);
            root = Utility.ParseAndAssert("<div><a>test</a><tag>def</div>",
                "P[<div>P[<a>P[test]</a><tag>P[def]</tag>]</div>]",
                options);
            Assert.True(((IWikitextParsingInfo)root.EnumDescendants().OfType<TagNode>().First(n => n.Name == "tag"))
                .InferredClosingMark);
            root = Utility.ParseAndAssert("<div><a>test</a><tag>def{{test|</div>",
                "P[<div>P[<a>P[test]</a><tag>P[def{{test|P[$</div$>]}}]</tag>]</div>]",
                options);
            Assert.True(((IWikitextParsingInfo) root.EnumDescendants().OfType<TagNode>().First(n => n.Name == "tag"))
                .InferredClosingMark);
        }
    }
}
