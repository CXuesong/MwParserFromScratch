using UnitTestProject1.Primitive;
using Xunit;
using Xunit.Abstractions;

namespace UnitTestProject1;

public class RegressionTests : ParserTestBase
{

    /// <inheritdoc />
    public RegressionTests(ITestOutputHelper output) : base(output)
    {
    }

    /// <summary>
    /// ToPlainText containing <br /> does not translate such tag in a \n new line
    /// </summary>
    [Fact]
    public void Issue24()
    {
        var root = this.ParseWikitext(
            "<span>Member of the [[Pennsylvania Senate]]<br/> from the [[Pennsylvania Senate, District 48|48th]] district</span>"
        );
        Assert.Equal("Member of the Pennsylvania Senate\n from the 48th district", root.ToPlainText());

        // Abnormal case 1 -- actually the second </br> won't be treated as closing tag, as <br> itself is self-closing.
        // See WikitextParserOptions.DefaultSelfClosingOnlyTags
        root = this.ParseWikitext("test<br>foo</br>bar");
        Assert.Equal("test\nfoo</br>bar", root.ToPlainText());
    }

}
