using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using Xunit;
using Xunit.Abstractions;

namespace UnitTestProject1.Primitive
{
    public class ParserTestBase
    {

        public ParserTestBase(ITestOutputHelper output)
        {
            Output = output;
        }

        public ITestOutputHelper Output { get; }

        public Wikitext ParseWikitext(string text)
        {
            var parser = new WikitextParser();
            var root = parser.Parse(text);
            return root;
        }

        /// <summary>
        /// Parses wikitext, and asserts
        /// 1. Whether the parsed AST can be converted back to the same wikitext as input.
        /// 2. Whether the parsed AST is correct.
        /// </summary>
        public Wikitext ParseAndAssert(string text, string expectedDump)
        {
            return ParseAndAssert(text, expectedDump, new WikitextParserOptions());
        }

        /// <summary>
        /// Parses wikitext, and asserts
        /// 1. Whether the parsed AST can be converted back to the same wikitext as input.
        /// 2. Whether the parsed AST is correct.
        /// </summary>
        public Wikitext ParseAndAssert(string text, string expectedDump, WikitextParserOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            var parser = new WikitextParser { Options = options };
            var root = parser.Parse(text);
            var parsedText = root.ToString();
            Output.WriteLine("=============================");
            Output.WriteLine("Original Text ---------------");
            Output.WriteLine(text);
            Output.WriteLine("Parsed Text -----------------");
            Output.WriteLine(parsedText);
            var rootExpr = Utility.Dump(root);
            Output.WriteLine("AST Dump --------------------");
            Output.WriteLine(EscapeString(rootExpr));
            Output.WriteLine("=============================");
            Assert.Equal(EscapeString(expectedDump), EscapeString(rootExpr));
            if (!options.AllowClosingMarkInference) Assert.Equal(text, parsedText);
            return root;
        }

        public string EscapeString(string str)
        {
            return str.Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        }

    }
}
