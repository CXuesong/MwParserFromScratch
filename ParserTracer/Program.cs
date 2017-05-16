using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;

namespace ParserTracer
{
    /// <summary>
    /// This program is used to trace the parsing process, used to debugging the parser.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                ShowHelp();
                return;
            }
            Console.WriteLine();
            var root = LoadAndParse(args[0]);
            Console.WriteLine("Parsed tree");
            PrintAst(root, 1);
        }

        static void ShowHelp()
        {
            Console.WriteLine(@"Usage:
ParserTracer.exe fileName

where
    fileName        is the path of the plain-text file containning the wikitext
                    to be parsed.
");
        }

        /// <summary>
        /// Loads a page from file and parse it.
        /// </summary>
        private static Wikitext LoadAndParse(string fileName)
        {
            var content = File.ReadAllText(fileName);
            var parser = new WikitextParser {Logger = new MyParserLogger()};
            return parser.Parse(content);
        }

        public static string Escapse(string expr)
        {
            return expr.Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private static void PrintAst(Node node, int level)
        {
            var indension = new string('.', level);
            var ns = node.ToString();
            Console.WriteLine("{0,-20} [{1}]", indension + node.GetType().Name,
                Escapse(ns.Substring(0, Math.Min(20, ns.Length))));
            foreach (var child in node.EnumChildren())
                PrintAst(child, level + 1);
        }
    }

    class MyParserLogger : IWikitextParserLogger
    {
        private class RegexStatistics
        {
            public int InvocationCount = 0;
            public long EllapsedTicks = 0;
            public TimeSpan Ellapsed => TimeSpan.FromTicks(EllapsedTicks);
            public TimeSpan AverageEllapsed => TimeSpan.FromTicks(EllapsedTicks/InvocationCount);
        }
        private readonly Dictionary<int, int> fallbackDict = new Dictionary<int, int>();
        private readonly Dictionary<string, RegexStatistics> regexStatDict = new Dictionary<string, RegexStatistics>();
        private int fallbackCounter;
        private readonly Stopwatch parserWatch = new Stopwatch();
        private readonly Stopwatch regexWatch = new Stopwatch();
        private string text;

        /// <inheritdoc />
        public void NotifyParsingStarted(string text)
        {
            this.text = text;
            fallbackDict.Clear();
            fallbackCounter = 0;
            parserWatch.Restart();
        }

        /// <inheritdoc />
        public void NotifyFallback(int offset, int contextStackSize)
        {
            if (!fallbackDict.TryGetValue(offset, out int counter)) counter = 0;
            fallbackDict[offset] = counter + 1;
            fallbackCounter++;
        }

        /// <inheritdoc />
        public void NotifyParsingFinished()
        {
            Console.WriteLine("Parsed {0} characters in {1} sec.", text.Length, parserWatch.Elapsed.TotalSeconds);
            Console.WriteLine("Regex Matching");
            Console.WriteLine("    Count  Elpsd ms   Average ms Expression");
            foreach (var fp in regexStatDict.OrderByDescending(p => p.Value.AverageEllapsed).Take(50))
            {
                Console.WriteLine("    {0,6} {1,10} {2,10} {3}", fp.Value.InvocationCount,
                        fp.Value.Ellapsed.TotalMilliseconds, fp.Value.AverageEllapsed.TotalMilliseconds,
                        Program.Escapse(fp.Key));
            }
            Console.WriteLine();
            Console.WriteLine("Fallbacks: {0}", fallbackCounter);
            Console.WriteLine("    Count  Position Text");
            foreach (var fp in fallbackDict.OrderByDescending(p => p.Value).Take(50))
            {
                var previewText = text.Substring(fp.Key);
                if (previewText.Length > 30) previewText = previewText.Substring(0, 30);
                previewText = previewText.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
                Console.WriteLine("    {0,6} {1,6} {2}", fp.Value, fp.Key, previewText);
            }
            regexStatDict.Clear();
            fallbackDict.Clear();
        }

        /// <inheritdoc />
        public void NotifyRegexMatchingStarted(int offset, Regex expression)
        {
            regexWatch.Restart();
        }

        /// <inheritdoc />
        public void NotifyRegexMatchingFinished(int offset, Regex expression)
        {
            if (!regexStatDict.TryGetValue(expression.ToString(), out RegexStatistics stat))
            {
                stat = new RegexStatistics();
                regexStatDict.Add(expression.ToString(), stat);
            }
            stat.EllapsedTicks += regexWatch.ElapsedTicks;
            stat.InvocationCount++;
        }
    }
}
