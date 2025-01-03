using System.Diagnostics;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using MwParserFromScratch.Rendering;
using UnitTestProject1.Primitive;
using Xunit;
using Xunit.Abstractions;

namespace UnitTestProject1;

public class NodeTests : ParserTestBase
{
    /// <inheritdoc />
    public NodeTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void EnumDescendantsTest()
    {
        // I'm lazy. That's all.
        var root = ParseAndAssert(
            "{{Translating|[[:en:Test]]|tpercent=20}}\n[[]]<div style=\"background: red\">{{T|Translating|source|3=tpercent=percentage of completion}}</div>",
            "P[{{Translating|P[[[:en:Test]]]|P[tpercent]=P[20]}}\n$[$[$]$]<div style=\"background: red\">P[{{T|P[Translating]|P[source]|P[3]=P[tpercent=percentage of completion]}}]</div>]");
        Trace.WriteLine("Descendants Dump:");
        foreach (var node in root.EnumDescendants())
        {
            var si = (IWikitextLineInfo)node;
            Assert.True(si.HasLineInfo);
            Trace.WriteLine(
                $"{node.GetType().Name}\t({si.StartLineNumber},{si.StartLinePosition})-({si.EndLineNumber},{si.EndLinePosition})\t[|{node}|]");
            if (node is IInlineContainer container)
            {
                IWikitextLineInfo lastChild = null;
                foreach (IWikitextLineInfo child in container.Inlines)
                {
                    if (lastChild != null)
                    {
                        if (lastChild.EndLineNumber == child.StartLineNumber)
                            Assert.True(lastChild.EndLinePosition == child.StartLinePosition, "LineInfo of Inline sequence is not consequent.");
                        else
                            Assert.True(child.StartLinePosition == 0, "LineInfo of Inline sequence is not consequent.");
                    }
                    lastChild = child;
                }
            }
        }
        var nn = root.Lines.FirstNode.NextNode;
        root.Lines.FirstNode.Remove();
        Assert.Equal(root.Lines.FirstNode, nn);
    }

    [Fact]
    public void TemplateArgumentsTest1()
    {
        var root = ParseWikitext("{{test|a|b}}");
        var t = root.EnumDescendants().OfType<Template>().First();
        Assert.Equal("a", t.Arguments[1].ToString());
        Assert.Equal("b", t.Arguments[2].ToString());
    }

    [Fact]
    public void TemplateArgumentsTest2()
    {
        var root = ParseWikitext("{{\ttest_T  |A=1|B=2|  c\n=3}}");
        var t = root.EnumDescendants().OfType<Template>().First();
        var arg2 = t.Arguments.ElementAt(2);
        Assert.Equal("Test T", MwParserUtility.NormalizeTitle(t.Name));
        Assert.Equal("c", MwParserUtility.NormalizeTemplateArgumentName(arg2.Name));
        t.Arguments["B"].Remove();
        Assert.Equal(2, t.Arguments.Count);
        Assert.Equal(arg2, t.Arguments.ElementAt(1));
    }

    [Fact]
    public void CustomPlainTextFormatterTest1()
    {
        var root = ParseWikitext("ここより{{Ruby|遥|はる}}か東に、かつて{{ruby|大国|たいこく}}があった。\n\n" +
                                 "千年の昔に{{ruby|滅|ほろ}}んで{{Ruby|無人|むじん}}となり、\n\n" +
                                 "以来\u3000立ち入りが禁じられている。\n");

        var pt = root.ToPlainText();
        Output.WriteLine(pt);
        Assert.Equal("ここよりか東に、かつてがあった。\n\n千年の昔にんでとなり、\n\n以来\u3000立ち入りが禁じられている。\n", root.ToPlainText());

        pt = root.ToPlainText(new RubyAwarePlainTextNodeRenderer());
        Output.WriteLine(pt);
        Assert.Equal("ここより遥（はる）か東に、かつて大国（たいこく）があった。\n\n千年の昔に滅（ほろ）んで無人（むじん）となり、\n\n以来\u3000立ち入りが禁じられている。\n", pt);

        pt = root.ToPlainText(new RubyAwarePlainTextNodeRenderer { HideRuby = true });
        Output.WriteLine(pt);
        Assert.Equal("ここより遥か東に、かつて大国があった。\n\n千年の昔に滅んで無人となり、\n\n以来\u3000立ち入りが禁じられている。\n", pt);
    }

    private class RubyAwarePlainTextNodeRenderer : PlainTextNodeRenderer
    {

        /// <summary>Whether to remove the ruby text (bracketed content) from the rendered plain text.</summary>
        public bool HideRuby { get; set; }

        /// <inheritdoc />
        protected override void RenderNode(Node node)
        {
            switch (node)
            {
                case Template t
                    when string.Equals(MwParserUtility.NormalizeTemplateArgumentName(t.Name), "Ruby",
                        StringComparison.OrdinalIgnoreCase):
                    // {{Ruby}}
                    // Render the annotated text first
                    if (t.Arguments[1]?.Value != null) RenderNode(t.Arguments[1].Value);

                    if (HideRuby) return;
                    // Then render the ruby text with brackets
                    OutputBuilder.Append('（');
                    var len = OutputBuilder.Length;
                    if (t.Arguments[2]?.Value != null) RenderNode(t.Arguments[2].Value);
                    if (OutputBuilder.Length > len)
                    {
                        OutputBuilder.Append('）');
                    }
                    else
                    {
                        // We can even decide to remove the L-bracket if we've realized ruby text is empty.
                        OutputBuilder.Remove(OutputBuilder.Length - 1, 1);
                    }
                    return;
            }

            base.RenderNode(node);
        }

    }
}
