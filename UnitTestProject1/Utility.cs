using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;

namespace UnitTestProject1
{
    internal static class Utility
    {
        private static Dictionary<Type, Func<Node, string>> dumpHandlers = new Dictionary<Type, Func<Node, string>>();

        private static void RegisterDumpHandler<T>(Func<T, string> handler) where T : Node
        {
            dumpHandlers.Add(typeof(T), n => handler((T) n));
        }

        static Utility()
        {
            // Add a $ mark before brackets to escape them
            RegisterDumpHandler<PlainText>(n => Regex.Replace(n.Content, @"(?=[\[\]\{\}])", "$"));
            RegisterDumpHandler<FormatSwitch>(fs =>
            {
                if (fs.SwitchBold && fs.SwitchItalics)
                    return "[BI]";
                if (fs.SwitchBold)
                    return "[B]";
                if (fs.SwitchItalics)
                    return "[I]";
                return "[]";
            });
            RegisterDumpHandler<WikiLink>(n => n.Text == null
                ? $"[[{Dump(n.Target)}]]"
                : $"[[{Dump(n.Target)}|{Dump(n.Text)}]]");
            RegisterDumpHandler<ExternalLink>(el =>
            {
                var s = el.ToString();
                // Add brackets to distinguish links form normal text.
                if (!el.Brackets) return "-[" + s + "]-";
                return s;
            });
            RegisterDumpHandler<Run>(w => string.Join(null, w.Inlines.Select(Dump)));
            RegisterDumpHandler<ListItem>(li => li.Prefix + "[" + string.Join(null, li.Inlines.Select(Dump)) + "]");
            RegisterDumpHandler<Heading>(h => $"H{h.Level}[{string.Join(null, h.Inlines.Select(Dump))}]");
            RegisterDumpHandler<Paragraph>(p => $"P[{string.Join(null, p.Inlines.Select(Dump))}]");
            RegisterDumpHandler<Wikitext>(w => string.Join(null, w.Lines.Select(Dump)));
            RegisterDumpHandler<ArgumentReference>(n =>
            {
                var s = "{{{" + Dump(n.Name);
                if (n.DefaultValue != null) s += "|" + Dump(n.DefaultValue);
                return s + "}}}";
            });
            RegisterDumpHandler<Template>(n =>
            {
                if (n.Arguments.IsEmpty) return "{{" + Dump(n.Name) + "}}";
                var sb = new StringBuilder("{{");
                sb.Append(n.Name);
                foreach (var arg in n.Arguments)
                {
                    sb.Append('|');
                    sb.Append(Dump(arg));
                }
                sb.Append("}}");
                return sb.ToString();
            });
            RegisterDumpHandler<TemplateArgument>(n =>
            {
                if (n.Name == null) return Dump(n.Value);
                return Dump(n.Name) + "=" + Dump(n.Value);
            });
            RegisterDumpHandler<Comment>(n => n.ToString());
        }

        public static Wikitext ParseWikitext(string text)
        {
            var parser = new WikitextParser();
            var root = parser.Parse(text);
            return root;
        }

        public static Wikitext ParseAndAssert(string text, string expectedExpression)
        {
            var parser = new WikitextParser();
            var root = parser.Parse(text);
            var rootExpr = Dump(root);
            Trace.WriteLine(rootExpr);
            Trace.WriteLine("");
            if (expectedExpression != rootExpr)
            {
                Assert.Fail("Expect: <{0}>, got: <{1}>.", EscapeString(expectedExpression), EscapeString(rootExpr));
            }
            return root;
        }

        public static string EscapeString(string str)
        {
            return str.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        }

        public static string Dump(Node node)
        {
            if (node == null) return null;
            return dumpHandlers[node.GetType()](node);
        }
    }
}
