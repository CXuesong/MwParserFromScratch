using System;
using System.Linq;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using UnitTestProject1.Primitive;
using Xunit;
using Xunit.Abstractions;

namespace UnitTestProject1;

public class ParsingBehaviorTests : ParserTestBase
{

    /// <inheritdoc />
    public ParsingBehaviorTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void TestMethod1()
    {
        ParseAndAssert("{{Test}}{{}}[[]][]", "P[{{Test}}${${$}$}$[$[$]$]$[$]]");
        ParseAndAssert("[[ ]]", "P[[[ ]]]");
        ParseAndAssert("{{Test}}{{}}[[]][]", "P[{{Test}}{{}}[[]][]]", new WikitextParserOptions
        {
            AllowEmptyTemplateName = true,
            AllowEmptyWikiLinkTarget = true,
            AllowEmptyExternalLinkTarget = true,
        });
    }

    [Fact]
    public void TestMethod2()
    {
        var options = new WikitextParserOptions
        {
            AllowClosingMarkInference = true
        };
        var root = ParseAndAssert("{{Test{{test|a|b|c}}|def|g=h",
            "P[{{Test{{test|a|b|c}}|P[def]|P[g]=P[h]}}]",
            options);
        Assert.True(((IWikitextParsingInfo)root.Lines.FirstNode.EnumChildren().First()).InferredClosingMark);
        root = ParseAndAssert("<div><a>test</a><tag>def</div>",
            "P[<div>P[<a>P[test]</a><tag>P[def]</tag>]</div>]",
            options);
        Assert.True(((IWikitextParsingInfo)root.EnumDescendants().OfType<TagNode>().First(n => n.Name == "tag"))
            .InferredClosingMark);
        root = ParseAndAssert("<div><a>test</a><tag>def{{test|</div>",
            "P[<div>P[<a>P[test]</a><tag>P[def{{test|P[$</div$>]}}]</tag>]</div>]",
            options);
        Assert.True(((IWikitextParsingInfo)root.EnumDescendants().OfType<TagNode>().First(n => n.Name == "tag"))
            .InferredClosingMark);
    }

}
