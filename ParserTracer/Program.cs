using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
            LoadAndParse(args[0]);
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
            var parser = new WikitextParser(null, new MyParserLogger());
            return parser.Parse(content);
        }
    }

    class MyParserLogger : IWikitextParserLogger
    {
        private readonly Dictionary<int, int> fallbackDict = new Dictionary<int, int>();
        private int fallbackCounter;
        private readonly Stopwatch sw = new Stopwatch();
        private string text;

        /// <inheritdoc />
        public void NotifyStartParsing(string text)
        {
            this.text = text;
            fallbackDict.Clear();
            fallbackCounter = 0;
            sw.Restart();
        }

        /// <inheritdoc />
        public void NotifyFallback(int offset, int contextStackSize)
        {
            int counter;
            if (!fallbackDict.TryGetValue(offset, out counter)) counter = 0;
            fallbackDict[offset] = counter + 1;
            fallbackCounter++;
        }

        /// <inheritdoc />
        public void NotifyStopParsing()
        {
            Console.WriteLine("Parsed {0} characters in {1} sec.", text.Length, sw.Elapsed.TotalSeconds);
            Console.WriteLine("Fallbacks: {0}", fallbackCounter);
            Console.WriteLine("    Count  Position Text");
            foreach (var fp in  fallbackDict.OrderByDescending(p => p.Value).Take(10))
            {
                var previewText = text.Substring(fp.Key);
                if (previewText.Length > 30) previewText = previewText.Substring(0, 30);
                previewText = previewText.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
                Console.WriteLine("    {0,6} {1,6} {2}", fp.Value, fp.Key, previewText);
            }
            fallbackDict.Clear();
        }
    }
}
