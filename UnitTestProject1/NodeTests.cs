using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
                "{{Translating|[[:en:Test]]|tpercent=20}}\n{{T|Translating|source|3=tpercent=percentage of completion}}",
                "P[{{Translating|P[[[:en:Test]]]|P[tpercent]=P[20]}}\n{{T|P[Translating]|P[source]|P[3]=P[tpercent=percentage of completion]}}]");
            Trace.WriteLine("Descendants Dump:");
            foreach (var node in root.EnumDescendants())
            {
                Trace.WriteLine(node.GetType().Name + " [|" + node + "|]");
            }
        }
    }
}
