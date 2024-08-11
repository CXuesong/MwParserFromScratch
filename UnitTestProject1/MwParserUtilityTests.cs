using System;
using MwParserFromScratch;
using UnitTestProject1.Primitive;
using Xunit;
using Xunit.Abstractions;

namespace UnitTestProject1;

public class MwParserUtilityTests : ParserTestBase
{

    /// <inheritdoc />
    public MwParserUtilityTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void NormalizeTitleTest1()
    {
        Assert.Equal("Test", MwParserUtility.NormalizeTitle(" \r\ntest\r\n "));
        Assert.Equal("Test test", MwParserUtility.NormalizeTitle(" \r\ntest test  _ "));
    }

}
