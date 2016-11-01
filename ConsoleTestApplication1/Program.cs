using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;

namespace ConsoleTestApplication1
{
    static class Program
    {
        private static string ReadInput()
        {
            var sb = new StringBuilder();
            int c;
            while ((c = Console.Read()) >= 0)
            {
                sb.Append((char)c);
            }
            return sb.ToString();
        }

        private static string Escapse(string expr)
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

        static void Main(string[] args)
        {
            SimpleDemo();
            ParseAndPrint();
        }

        static void SimpleDemo()
        {
            // Fill the missing template parameters.
            var parser = new WikitextParser();
            var templateNames = new [] {"Expand section", "Cleanup"};
            var text = @"==Hello==<!--comment-->
{{Expand section|date=2010-10-05}}
{{Cleanup}}
This is a nice '''paragraph'''.
==References==
{{Reflist}}
";
            var ast = parser.Parse(text);
            // Convert the code snippets to nodes
            var dateName = parser.Parse("date");
            var dateValue = parser.Parse(DateTime.Now.ToString("yyyy-MM-dd"));
            // Find and set
            foreach (var t in ast.EnumDescendants().OfType<Template>()
                .Where(t => templateNames.Contains(t?.Name.ToString().Trim())))
            {
                var date = t.Arguments.FirstOrDefault(a => a.Name?.ToString() == "date");
                if (date == null)
                {
                    date = new TemplateArgument {Name = dateName};
                    t.Arguments.Add(date);
                }
                date.Value = dateValue;
            }
            Console.WriteLine(ast.ToString());
        }

        private static void ParseAndPrint()
        {
            var parser = new WikitextParser();
            Console.WriteLine("Please input the wikitext to parse, use EOF (Ctrl+Z) to accept:");
            var ast = parser.Parse(ReadInput());
            Console.WriteLine("Parsed AST");
            PrintAst(ast, 0);
        }
    }
}
