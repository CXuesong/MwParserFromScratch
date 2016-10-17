using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MwParserFromScratch.Nodes;

namespace MwParserFromScratch
{
    partial class WikitextParser
    {
        /// <summary>
        /// This function is intended to handle braces.
        /// </summary>
        public InlineNode ParseBraces()
        {
            InlineNode node;
            // Known Issue: The derivation is ambiguous for {{{{T}}}} .
            // Current implementation will treat it as {{{ {T }}} }, where {T is rendered as normal text,
            // while actually it should be treated as { {{{T}}} } .
            var lbraces = LookAheadToken(@"\{+");
            if (lbraces == null || lbraces.Length < 2) return null;
            // For {{{{T}}}}
            // fallback so that RUN can parse the first brace as PLAIN_TEXT
            if (lbraces.Length == 4) return null;
            // For {{{{{T}}}}}, treat it as {{ {{{T}}} }} first
            if (lbraces.Length == 5)
            {
                if ((node = ParseTemplate()) != null) return node;
                // If it failed, we just go to normal routine.
                // E.g. {{{{{T}}, or {{{{{T
            }
            // We're facing a super abnormal case, like {{{{{{T}}}}}}
            // It seems that in most cases, MediaWiki will just print them out.
            // TODO Consider this thoroughly.
            if (lbraces.Length > 5)
            {
                ParseStart();
                // Consume all the l-braces
                lbraces = ConsumeToken(@"\{+");
                return ParseSuccessful(new PlainText(lbraces));
            }
            if ((node = ParseArgumentReference()) != null) return node;
            if ((node = ParseTemplate()) != null) return node;
            return null;
        }

        /// <summary>
        /// ARGUMENT_REF
        /// </summary>
        private ArgumentReference ParseArgumentReference()
        {
            ParseStart(@"\}\}\}|\|", true);
            if (ConsumeToken(@"\{\{\{") == null)
                return ParseFailed<ArgumentReference>();
            var name = ParseWikitext();
            Debug.Assert(name != null);
            var defaultValue = ConsumeToken(@"\|") != null ? ParseWikitext() : null;
            // For {{{A|b|c}}, we should consume and discard c .
            // Parsing is still needed in order to handle the cases like {{{A|b|c{{T}}}}}
            while (ConsumeToken(@"\|") != null)
                ParseWikitext();
            if (ConsumeToken(@"\}\}\}") == null)
                return ParseFailed<ArgumentReference>();
            return ParseSuccessful(new ArgumentReference(name, defaultValue));
        }

        /// <summary>
        /// TEMPLATE
        /// </summary>
        private Template ParseTemplate()
        {
            ParseStart(@"\}\}|\|", true);
            if (ConsumeToken(@"\{\{") == null)
                return ParseFailed<Template>();
            var name = new Run();
            if (!ParseRun(RunParsingMode.ExpandableText, name))
                return ParseFailed<Template>();
            var node = new Template(name);
            while (ConsumeToken(@"\|") != null)
            {
                var arg = ParseTemplateArgument();
                node.Arguments.Add(arg);
            }
            if (ConsumeToken(@"\}\}") == null) return ParseFailed<Template>();
            return ParseSuccessful(node);
        }

        /// <summary>
        /// TEMPLATE_ARG
        /// </summary>
        private TemplateArgument ParseTemplateArgument()
        {
            ParseStart(@"=", false);
            var a = ParseWikitext();
            Debug.Assert(a != null);
            if (ConsumeToken(@"=") != null)
            {
                // name=value
                CurrentContext.Terminator = null;
                var value = ParseWikitext();
                Debug.Assert(value != null);
                return ParseSuccessful(new TemplateArgument(a, value));
            }
            return ParseSuccessful(new TemplateArgument(null, a));
        }
    }
}