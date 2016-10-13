using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;

namespace UnitTestProject1
{
    internal static class Utility
    {
        public static Wikitext ParseWikitext(string text)
        {
            var parser = new WikitextParser();
            var root = parser.Parse(text);
            return root;
        }

        public static Wikitext ParseAndAssert(string text, string expectedExpression)
        {
            var parser = new WikitextParser();
            var root = parser.Parse(text);
            var rootExpr = root.ToString();
            Trace.WriteLine(rootExpr);
            Assert.AreEqual(expectedExpression, rootExpr);
            return root;
        }
    }
}
