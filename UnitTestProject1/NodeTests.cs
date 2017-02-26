using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MwParserFromScratch;

namespace UnitTestProject1
{
    [TestClass]
    public class NodeTests
    {
        [TestMethod]
        public void EnumDescendantsTest()
        {
            // I'm lazy. That's all.
            var root = Utility.ParseAndAssert(
                "{{Translating|[[:en:Test]]|tpercent=20}}\n<div style=\"background: red\">{{T|Translating|source|3=tpercent=percentage of completion}}</div>",
                "P[{{Translating|P[[[:en:Test]]]|P[tpercent]=P[20]}}\n<div style=\"background: red\">P[{{T|P[Translating]|P[source]|P[3]=P[tpercent=percentage of completion]}}]</div>]");
            Trace.WriteLine("Descendants Dump:");
            foreach (var node in root.EnumDescendants())
            {
                var li = (IWikitextLineInfo) node;
                var si = (IWikitextSpanInfo) node;
                Trace.WriteLine(
                    $"{node.GetType().Name}\t({li.LineNumber},{li.LinePosition};{si.Start}+{si.Length})\t[|{node}|]");
            }
        }
    }
}
