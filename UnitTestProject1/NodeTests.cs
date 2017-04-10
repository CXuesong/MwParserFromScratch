using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;

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
            var nn = root.Lines.FirstNode.NextNode;
            root.Lines.FirstNode.Remove();
            Assert.AreSame(root.Lines.FirstNode, nn);
        }

        [TestMethod]
        public void TemplateArgumentsTest()
        {
            var root = Utility.ParseWikitext("{{\ttest_T  |A=1|B=2|  c\n=3}}");
            var t = root.EnumDescendants().OfType<Template>().First();
            var arg2 = t.Arguments.ElementAt(2);
            Assert.AreEqual("Test T", MwParserUtility.NormalizeTitle(t.Name));
            Assert.AreEqual("c", MwParserUtility.NormalizeTemplateArgumentName(arg2.Name));
            t.Arguments["B"].Remove();
            Assert.AreEqual(2, t.Arguments.Count);
            Assert.AreEqual(arg2, t.Arguments.ElementAt(1));
        }
    }
}
