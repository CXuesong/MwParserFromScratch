using System;
using System.Collections.Generic;
using System.Text;
using MwParserFromScratch.Nodes;
using MwParserFromScratch.Rendering;
using MwParserFromScratch;
using UnitTestProject1.Primitive;
using Xunit;
using Xunit.Abstractions;

namespace UnitTestProject1;

public class PlainTextFormattingTests : ParserTestBase
{

    /// <inheritdoc />
    public PlainTextFormattingTests(ITestOutputHelper output) : base(output)
    {
    }

    private void AssertPlainText(string wikitext, string expectedPlainText)
    {
        var root = ParseWikitext(wikitext);
        Assert.Equal(expectedPlainText, root.ToPlainText());
    }

    [Fact]
    public void BasicContent()
    {
        AssertPlainText("Line\n1\n\nLine2\n\nLine3", "Line\n1\n\nLine2\n\nLine3");
        AssertPlainText("# item1\n# item2\n#item3", " item1\n item2\nitem3");
        AssertPlainText(@"eq 1-1: <math>\frac{1}{2}</math> <ref>citation</ref>", "eq 1-1:  ");
        AssertPlainText("header<span>content</span><div>main&nbsp;content</div><pre>footer</pre>", "headercontentmain contentfooter");
    }


    [Fact]
    public void CustomPlainTextNodeRendererTest1()
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
