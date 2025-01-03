using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using MwParserFromScratch.Nodes;

namespace UnitTestProject1;

internal static class Utility
{

    private static readonly Dictionary<Type, Func<Node, string>> dumpHandlers = new();
        
    private static void RegisterDumpHandler<T>(Func<T, string> handler) where T : Node
    {
        dumpHandlers.Add(typeof(T), n => handler((T) n));
    }

    static Utility()
    {
        // Add a $ mark before brackets to escape them
        RegisterDumpHandler<PlainText>(n => Regex.Replace(n.Content, @"(?=[\[\]\{\}<>])", "$"));
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
        RegisterDumpHandler<WikiImageLink>(n =>
        {
            if (n.Arguments.Count == 0) return "![[" + Dump(n.Target) + "]]";
            var sb = new StringBuilder("![[");
            sb.Append(n.Target);
            foreach (var arg in n.Arguments)
            {
                sb.Append('|');
                sb.Append(Dump(arg));
            }
            sb.Append("]]");
            return sb.ToString();
        });
        RegisterDumpHandler<WikiImageLinkArgument>(n =>
        {
            if (n.Name == null) return Dump(n.Value);
            return Dump(n.Name) + "=" + Dump(n.Value);
        });
        RegisterDumpHandler<ExternalLink>(el =>
        {
            var s = el.ToString();
            // Add brackets to distinguish links form normal text.
            if (!el.Brackets) return "-[" + s + "]-";
            return s;
        });
        RegisterDumpHandler<Run>(w => string.Join(null, w.Inlines.Select(Dump)));
        RegisterDumpHandler<ListItem>(li => li.Prefix + "[" + string.Join(null, li.Inlines.Select(Dump)) + "]");
        RegisterDumpHandler<Heading>(h =>
        {
            var expr = $"H{h.Level}[{string.Join(null, h.Inlines.Select(Dump))}]";
            if (h.Suffix != null) expr += "[" + h.Suffix + "]";
            return expr;
        });
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
            if (n.Arguments.Count == 0) return "{{" + Dump(n.Name) + "}}";
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
        Func<TagNode, string> tagNodeHandler = n =>
        {
            var sb = new StringBuilder("<");
            sb.Append(n.Name);
            sb.Append(string.Join(null, n.Attributes.Select(Dump)));
            sb.Append(n.Attributes.TrailingWhitespace);
            switch (n.TagStyle)
            {
                case TagStyle.Normal:
                case TagStyle.NotClosed:
                    sb.Append('>');
                    var pt = n as ParserTag;
                    if (pt != null) sb.Append(pt.Content);
                    var ht = n as HtmlTag;
                    if (ht != null) sb.Append(Dump(ht.Content));
                    break;
                case TagStyle.SelfClosing:
                    sb.Append("/>");
                    return sb.ToString();
                case TagStyle.CompactSelfClosing:
                    sb.Append(">");
                    return sb.ToString();
                default:
                    Debug.Assert(false);
                    break;
            }
            sb.Append("</");
            sb.Append(n.ClosingTagName ?? n.Name);
            sb.Append(n.ClosingTagTrailingWhitespace);
            sb.Append('>');
            return sb.ToString();
        };
        RegisterDumpHandler<ParserTag>(tagNodeHandler);
        RegisterDumpHandler<HtmlTag>(tagNodeHandler);
        RegisterDumpHandler<TagAttribute>(n =>
        {
            string quote;
            switch (n.Quote)
            {
                case ValueQuoteType.None:
                    quote = null;
                    break;
                case ValueQuoteType.SingleQuotes:
                    quote = "'";
                    break;
                case ValueQuoteType.DoubleQuotes:
                    quote = "\"";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return n.LeadingWhitespace + n.Name + n.WhitespaceBeforeEqualSign + "="
                   + n.WhitespaceAfterEqualSign + quote + n.Value + quote;
        });
    }

    public static string Dump(Node node)
    {
        if (node == null) return null;
        return dumpHandlers[node.GetType()](node);
    }
}
