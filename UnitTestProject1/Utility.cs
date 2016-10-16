using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;

namespace UnitTestProject1
{
    internal static class Utility
    {
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
                Assert.Fail("Expect:\n{0}\n, got\n{1}\n.", EscapeString(expectedExpression), EscapeString(rootExpr));
            }
            return root;
        }

        public static string EscapeString(string str)
        {
            return str.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        }

        public static string Dump(Node node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            var pt = node as PlainText;
            if (pt != null) return pt.Content.Replace("[", @"$[").Replace("]", @"$]");
            var fs = node as FormatSwitch;
            if (fs != null)
            {
                if (fs.SwitchBold && fs.SwitchItalics)
                    return "[BI]";
                if (fs.SwitchBold)
                    return "[B]";
                if (fs.SwitchItalics)
                    return "[I]";
                return "[]";
            }
            var el = node as ExternalLink;
            if (el != null)
            {
                var s = el.ToString();
                // Add brackets to distinguish links form normal text.
                if (!el.Brackets) return "-[" + s + "]-";
            }
            var li = node as ListItem;
            if (li != null)
                return li.Prefix + "[" + string.Join(null, li.Inlines.Select(Dump)) + "]";
            var h = node as Heading;
            if (h != null)
                return $"H{h.Level}[{string.Join(null, h.Inlines.Select(Dump))}]";
            var p = node as Paragraph;
            if (p != null)
                return $"P[{string.Join(null, p.Inlines.Select(Dump))}]";
            var w = node as Wikitext;
            if (w != null)
                return string.Join(null, w.Lines.Select(Dump));
            return node.ToString();
        }
    }
}
