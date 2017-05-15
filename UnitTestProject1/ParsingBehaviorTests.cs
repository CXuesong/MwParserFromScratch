using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MwParserFromScratch;

namespace UnitTestProject1
{
    [TestClass]
    public class ParsingBehaviorTests
    {
        [TestMethod]
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
    }
}
