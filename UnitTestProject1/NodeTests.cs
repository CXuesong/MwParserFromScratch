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
                "{{Translating|[[:en:Test]]|tpercent=20}}\n[[]]<div style=\"background: red\">{{T|Translating|source|3=tpercent=percentage of completion}}</div>",
                "P[{{Translating|P[[[:en:Test]]]|P[tpercent]=P[20]}}\n$[$[$]$]<div style=\"background: red\">P[{{T|P[Translating]|P[source]|P[3]=P[tpercent=percentage of completion]}}]</div>]");
            Trace.WriteLine("Descendants Dump:");
            foreach (var node in root.EnumDescendants())
            {
                var si = (IWikitextLineInfo) node;
                Assert.IsTrue(si.HasLineInfo);
                Trace.WriteLine(
                    $"{node.GetType().Name}\t({si.StartLineNumber},{si.StartLinePosition})-({si.EndLineNumber},{si.EndLinePosition})\t[|{node}|]");
                if (node is IInlineContainer container)
                {
                    IWikitextLineInfo lastChild = null;
                    foreach (IWikitextLineInfo child in container.Inlines)
                    {
                        if (lastChild != null)
                        {
                            if (lastChild.EndLineNumber == child.StartLineNumber)
                                Assert.AreEqual(lastChild.EndLinePosition, child.StartLinePosition, "LineInfo of Inline sequence is not consequent.");
                            else
                                Assert.AreEqual(0, child.StartLinePosition, "LineInfo of Inline sequence is not consequent.");
                        }
                        lastChild = child;
                    }
                }
            }
            var nn = root.Lines.FirstNode.NextNode;
            root.Lines.FirstNode.Remove();
            Assert.AreSame(root.Lines.FirstNode, nn);
        }

        [TestMethod]
        public void TemplateArgumentsTest1()
        {
            var root = Utility.ParseWikitext("{{test|a|b}}");
            var t = root.EnumDescendants().OfType<Template>().First();
            Assert.AreEqual("a", t.Arguments[1].ToString());
            Assert.AreEqual("b", t.Arguments[2].ToString());
        }

        [TestMethod]
        public void TemplateArgumentsTest2()
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
