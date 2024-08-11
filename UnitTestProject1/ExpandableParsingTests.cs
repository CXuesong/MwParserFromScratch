using System;
using System.Diagnostics;
using System.Linq;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using UnitTestProject1.Primitive;
using Xunit;
using Xunit.Abstractions;

namespace UnitTestProject1;

public class ExpandableParsingTests : ParserTestBase
{

    /// <inheritdoc />
    public ExpandableParsingTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void TestArgumentRef1()
    {
        var root = ParseAndAssert("Value is {{{1}}}.\n", "P[Value is {{{P[1]}}}.\n]");
    }

    [Fact]
    public void TestArgumentRef2()
    {
        var root = ParseAndAssert("Value is {{{1|Default value}}}.\n", "P[Value is {{{P[1]|P[Default value]}}}.\n]");
    }

    [Fact]
    public void TestArgumentRef3()
    {
        var root = ParseAndAssert("Value is {{{1\n2|Default value\n\nAnother paragraph!\n}}}.\n",
            "P[Value is {{{P[1\n2]|P[Default value\n]P[Another paragraph!\n]}}}.\n]");
    }

    [Fact]
    public void TestArgumentRef4()
    {
        var root = ParseAndAssert("Link is {{{link\n|[[Default link|link text]]}}}.",
            "P[Link is {{{P[link\n]|P[[[Default link|link text]]]}}}.]");
    }

    [Fact]
    public void TestArgumentRef5()
    {
        var root = ParseAndAssert("Link is {{{link\n|[[Default link|{{{text|default text}}}]]}}}.",
            "P[Link is {{{P[link\n]|P[[[Default link|{{{P[text]|P[default text]}}}]]]}}}.]");
    }

    [Fact]
    public void TestTemplate1()
    {
        var root = ParseAndAssert(
            "{{disambig}}\nTest may refer to\n* Test and experiment.\n* The River Test.\n",
            "P[{{disambig}}\nTest may refer to]*[ Test and experiment.]*[ The River Test.]P[]");
        var dab = root.EnumDescendants().OfType<Template>()
            .First(t => MwParserUtility.NormalizeTitle(t.Name) == "Disambig");
        Assert.Empty(dab.Arguments);
    }

    [Fact]
    public void TestTemplate2()
    {
        var root = ParseAndAssert(
            "{{Translating|[[:en:Test]]|\ntpercent=20}}\n{{T|Translating|source|3=tpercent=percentage of completion}}",
            "P[{{Translating|P[[[:en:Test]]]|P[\ntpercent]=P[20]}}\n{{T|P[Translating]|P[source]|P[3]=P[tpercent=percentage of completion]}}]");
        var trans = root.EnumDescendants().OfType<Template>()
            .First(t => MwParserUtility.NormalizeTitle(t.Name) == "Translating");
        Assert.Equal(2, trans.Arguments.Count);
        Assert.Equal("[[:en:Test]]", trans.Arguments[1].Value.ToString());
        Assert.Equal("20", trans.Arguments["  tpercent   "].Value.ToString());
    }

    [Fact]
    public void TestTemplate3()
    {
        var root = ParseAndAssert("{{Template |a = 10 |b = \n |c=20}}", "P[{{Template |P[a ]=P[ 10 ]|P[b ]=P[ \n ]|P[c]=P[20]}}]");
    }

    [Fact]
    public void TestMagicWords1()
    {
        var root = ParseAndAssert("{{ \t #if:Y|Yes|No}}", "P[{{ \t #if|P[Y]|P[Yes]|P[No]}}]");
        Assert.True(root.EnumDescendants().OfType<Template>().First().IsMagicWord);
        root = ParseAndAssert("{{ PAGESINCATEGORY :categoryname }}", "P[{{ PAGESINCATEGORY |P[categoryname ]}}]");
        Assert.True(root.EnumDescendants().OfType<Template>().First().IsMagicWord);
        root = ParseAndAssert("{{filepAth:  Wiki.png}}", "P[{{filepAth|P[  Wiki.png]}}]");
        Assert.True(root.EnumDescendants().OfType<Template>().First().IsMagicWord);
    }

    [Fact]
    public void TestBraces1()
    {
        var root = ParseAndAssert("{{Foo|{{Bar}}{{{Buzz}}}}}", "P[{{Foo|P[{{Bar}}{{{P[Buzz]}}}]}}]");
    }

    [Fact]
    public void TestTag1()
    {
        var root = ParseAndAssert(
            "Text<div style\t=\r\n\"background: red;\">Block</div>Test\n",
            "P[Text<div style\t=\r\n\"background: red;\">P[Block]</div>Test\n]");
    }

    [Fact]
    public void TestTag2()
    {
        var root = ParseAndAssert(
            "Text<div style\t=\r\n\"background: red;\">Block <div title\n\n=\n\n\"text\">Hint\n\nHint</div></div>Test\n",
            "P[Text<div style\t=\r\n\"background: red;\">P[Block <div title\n\n=\n\n\"text\">P[Hint\n]P[Hint]</div>]</div>Test\n]");
    }

    [Fact]
    public void TestTag3()
    {
        var root = ParseAndAssert(
            ";Gallery\n<hr /><gallery mode=packed>Image1.png|Caption1\nImage2.png|Caption2</gallery>",
            ";[Gallery]P[<hr /><gallery mode=packed>Image1.png|Caption1\nImage2.png|Caption2</gallery>]");
    }

    [Fact]
    public void TestTag4()
    {
        var root = ParseAndAssert(
            "Text<ref group='a'>reference</ref>\n==Citations==\n<references group=a  />",
            "P[Text<ref group='a'>reference</ref>]H2[Citations]P[<references group=a  />]");
    }

    [Fact]
    public void TestTag5()
    {
        var root = ParseAndAssert(
            "<div>test</DIV><ref>text</ reF>",
            "P[<div>P[test]</DIV>$<ref$>text$</ reF$>]");
    }

    [Fact]
    public void TestTag6()
    {
        ParseAndAssert("header<li>item 1\n\ncontent</li>", "P[header<li>P[item 1\n]P[content]</li>]");
        ParseAndAssert("header<li>item 1<div>content\n\ncontent</div></li>", "P[header<li>P[item 1<div>P[content\n]P[content]</div>]</li>]");
    }

    [Fact]
    public void TestHeading1()
    {
        var root = ParseAndAssert(
            "==Title==Title{{Template|==}}abc=={{Def}}<!--comm-->",
            "H2[Title==Title{{Template|=P[=]}}abc][{{Def}}<!--comm-->]");
    }
}
