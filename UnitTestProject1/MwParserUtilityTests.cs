using System;
using MwParserFromScratch;
using Xunit;

namespace UnitTestProject1
{
    public class MwParserUtilityTests
    {
        [Fact]
        public void NormalizeTitleTest1()
        {
            Assert.Equal("Test", MwParserUtility.NormalizeTitle(" \r\ntest\r\n "));
            Assert.Equal("Test test", MwParserUtility.NormalizeTitle(" \r\ntest test  _ "));
        }
    }
}
