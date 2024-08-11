using System;
using System.Collections.Generic;
using System.Text;
using UnitTestProject1.Primitive;
using Xunit;
using Xunit.Abstractions;

namespace UnitTestProject1;

public class PlainTextFormattingTests: ParserTestBase
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

}
