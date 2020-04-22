using System;
using System.Diagnostics;
using System.Linq;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using UnitTestProject1.Primitive;
using Xunit;
using Xunit.Abstractions;

namespace UnitTestProject1
{
    public class NodeTests : ParserTestBase
    {
        /// <inheritdoc />
        public NodeTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void EnumDescendantsTest()
        {
            // I'm lazy. That's all.
            var root = ParseAndAssert(
                "{{Translating|[[:en:Test]]|tpercent=20}}\n[[]]<div style=\"background: red\">{{T|Translating|source|3=tpercent=percentage of completion}}</div>",
                "P[{{Translating|P[[[:en:Test]]]|P[tpercent]=P[20]}}\n$[$[$]$]<div style=\"background: red\">P[{{T|P[Translating]|P[source]|P[3]=P[tpercent=percentage of completion]}}]</div>]");
            Trace.WriteLine("Descendants Dump:");
            foreach (var node in root.EnumDescendants())
            {
                var si = (IWikitextLineInfo)node;
                Assert.True(si.HasLineInfo);
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
                                Assert.True(lastChild.EndLinePosition == child.StartLinePosition, "LineInfo of Inline sequence is not consequent.");
                            else
                                Assert.True(child.StartLinePosition == 0, "LineInfo of Inline sequence is not consequent.");
                        }
                        lastChild = child;
                    }
                }
            }
            var nn = root.Lines.FirstNode.NextNode;
            root.Lines.FirstNode.Remove();
            Assert.Equal(root.Lines.FirstNode, nn);
        }

        [Fact]
        public void TemplateArgumentsTest1()
        {
            var root = ParseWikitext("{{test|a|b}}");
            var t = root.EnumDescendants().OfType<Template>().First();
            Assert.Equal("a", t.Arguments[1].ToString());
            Assert.Equal("b", t.Arguments[2].ToString());
        }

        [Fact]
        public void TemplateArgumentsTest2()
        {
            var root = ParseWikitext("{{\ttest_T  |A=1|B=2|  c\n=3}}");
            var t = root.EnumDescendants().OfType<Template>().First();
            var arg2 = t.Arguments.ElementAt(2);
            Assert.Equal("Test T", MwParserUtility.NormalizeTitle(t.Name));
            Assert.Equal("c", MwParserUtility.NormalizeTemplateArgumentName(arg2.Name));
            t.Arguments["B"].Remove();
            Assert.Equal(2, t.Arguments.Count);
            Assert.Equal(arg2, t.Arguments.ElementAt(1));
        }
    }
}
