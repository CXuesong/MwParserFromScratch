using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MwParserFromScratch;

namespace UnitTestProject1
{
    [TestClass]
    public class MwParserUtilityTests
    {
        [TestMethod]
        public void NormalizeTitleTest1()
        {
            Assert.AreEqual("Test", MwParserUtility.NormalizeTitle(" \r\ntest\r\n "));
            Assert.AreEqual("Test test", MwParserUtility.NormalizeTitle(" \r\ntest test  _ "));

        }
    }
}
