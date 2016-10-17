using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1
{
    [TestClass]
    public class TemplateParsingTests
    {
        [TestMethod]
        public void TestArgumentRef1()
        {
            var root = Utility.ParseAndAssert("Value is {{{1}}}.\n", "P[Value is {{{P[1]}}}.\n]");
        }

        [TestMethod]
        public void TestArgumentRef2()
        {
            var root = Utility.ParseAndAssert("Value is {{{1|Default value}}}.\n", "P[Value is {{{P[1]|P[Default value]}}}.\n]");
        }

        [TestMethod]
        public void TestArgumentRef3()
        {
            var root = Utility.ParseAndAssert("Value is {{{1\n2|Default value\n\nAnother paragraph!\n}}}.\n",
                "P[Value is {{{P[1\n2]|P[Default value\n]P[Another paragraph!\n]}}}.\n]");
        }

        [TestMethod]
        public void TestArgumentRef4()
        {
            var root = Utility.ParseAndAssert("Link is {{{link\n|[[Default link|link text]]}}}.",
                "P[Link is {{{P[link\n]|P[[[Default link|link text]]]}}}.]");
        }

        [TestMethod]
        public void TestArgumentRef5()
        {
            var root = Utility.ParseAndAssert("Link is {{{link\n|[[Default link|{{{text|default text}}}]]}}}.",
                "P[Link is {{{P[link\n]|P[[[Default link|{{{P[text]|P[default text]}}}]]]}}}.]");
        }

        [TestMethod]
        public void TestTemplate1()
        {
            var root = Utility.ParseAndAssert(
                "{{Disambig}}\nTest may refer to\n* Test and experiment.\n* The River Test.\n",
                "P[{{Disambig}}\nTest may refer to]*[ Test and experiment.]*[ The River Test.]P[]");
        }

        [TestMethod]
        public void TestTemplate2()
        {
            var root = Utility.ParseAndAssert(
                "{{Translating|[[:en:Test]]|tpercent=20}}\n{{T|Translating|source|3=tpercent=percentage of completion}}",
                "P[{{Translating|P[[[:en:Test]]]|P[tpercent]=P[20]}}\n{{T|P[Translating]|P[source]|P[3]=P[tpercent=percentage of completion]}}]");
        }

        [TestMethod]
        public void TestBraces1()
        {
            var root = Utility.ParseAndAssert("{{Foo|{{Bar}}{{{Buzz}}}}}", "P[{{Foo|P[{{Bar}}{{{P[Buzz]}}}]}}]");
        }
    }
}
