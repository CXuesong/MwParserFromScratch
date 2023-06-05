using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MwParserFromScratch.Nodes;

namespace MwParserFromScratch
{
    partial class ParserCore
    {
        private static readonly Regex commentSuffixMatcher = new Regex("-->");
        private static readonly Dictionary<string, Regex> closingTagMatcherCache = new Dictionary<string, Regex>();

        private Comment ParseComment()
        {
            ParseStart();
            if (ConsumeToken("<!--") == null) return ParseFailed<Comment>();
            var contentPos = position;
            var suffix = commentSuffixMatcher.Match(fulltext, position);
            if (suffix.Success)
            {
                MovePositionTo(suffix.Index + suffix.Length);
                return ParseSuccessful(new Comment(fulltext.Substring(contentPos, suffix.Index - contentPos)));
            }
            MovePositionTo(fulltext.Length);
            return ParseSuccessful(new Comment(fulltext.Substring(contentPos)));
        }

        /// <summary>
        /// This function is intended to handle braces.
        /// </summary>
        private InlineNode ParseBraces()
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
            // Look ahead to determine the Magic Words
            var node = new Template(new Run());
            if (LookAheadToken(@"\s*#") != null)
            {
                node.IsMagicWord = true;
            }
            else
            {
                var litName = LookAheadToken(@"\s*[^:\|\{\}]+(?=[:\}])");
                if (litName != null)
                {
                    litName = litName.Trim();
                    node.IsMagicWord = options.CaseSensitiveMagicTemplateNamesSet.Contains(litName) ||
                                       options.CaseInsensitiveMagicTemplateNamesSet.Contains(litName);
                }
            }
            if (node.IsMagicWord)
            {
                ParseStart(":", false);
                if (!ParseRun(RunParsingMode.ExpandableText, node.Name, true))
                {
                    Debug.Assert(false, "We have ensured we can read something. Wierd.");
                    Fallback();
                    return ParseFailed(node);
                }
                Accept();
                Debug.Assert(LookAheadToken(@"\|") == null);
                if (ConsumeToken(":") != null)
                {
                    // Parse 1st argument.
                    node.Arguments.Add(ParseTemplateArgument());
                }
            }
            else
            {
                if (!ParseRun(RunParsingMode.ExpandableText, node.Name, true))
                {
                    if (options.AllowEmptyTemplateName)
                        node.Name = null;
                    else
                        return ParseFailed(node);
                }
            }
            while (ConsumeToken(@"\|") != null)
            {
                var arg = ParseTemplateArgument();
                node.Arguments.Add(arg);
            }
            if (ConsumeToken(@"\}\}") == null)
            {
                if (options.AllowClosingMarkInference)
                    node.SetInferredClosingMark();
                else
                    return ParseFailed(node);
            }
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

                MwParserUtility.NormalizeTemplateArgumentText(value);

                return ParseSuccessful(new TemplateArgument(a, value));
            }
            return ParseSuccessful(new TemplateArgument(null, a));
        }

        private TagNode ParseTag()
        {
            ParseStart();
            if (ConsumeToken("<") == null) return ParseFailed<TagNode>();
            var tagName = ConsumeToken(@"[\w-_:]+");
            if (tagName == null) return ParseFailed<TagNode>();
            var node = options.ParserTagsSet.Contains(tagName) ? (TagNode)new ParserTag(tagName) : new HtmlTag(tagName);
            string rbracket;
            var ws = ConsumeToken(@"\s+");
            // TAG_ATTR
            while ((rbracket = ConsumeToken("/?>")) == null)
            {
                // We need some whitespace to delimit the attributes.
                if (ws == null)
                {
                    // This is a pathological condition.
                    // E.g. <ref abc[EOF]
                    // or   {{T|<ref def}}
                    return ParseFailed<TagNode>();
                }
                // If attrName == null, then we have something like <tag =abc >, which is still valid (attributeName = "").
                ParseStart();
                var attrName = ParseAttributeName();
                var attr = new TagAttribute { Name = attrName, LeadingWhitespace = ws };
                ws = ConsumeToken(@"\s+");
                if (ConsumeToken("=") != null)
                {
                    attr.WhitespaceBeforeEqualSign = ws;
                    attr.WhitespaceAfterEqualSign = ConsumeToken(@"\s+");
                    if ((attr.Value = ParseAttributeValue(ValueQuoteType.SingleQuotes)) != null)
                        attr.Quote = ValueQuoteType.SingleQuotes;
                    else if ((attr.Value = ParseAttributeValue(ValueQuoteType.DoubleQuotes)) != null)
                        attr.Quote = ValueQuoteType.DoubleQuotes;
                    else
                    {
                        attr.Value = ParseAttributeValue(ValueQuoteType.None);
                        attr.Quote = ValueQuoteType.None;
                        Debug.Assert(attr.Value != null,
                            "ParseAttributeValue(ValueQuoteType.None) should always be successful.");
                    }
                    ws = ConsumeToken(@"\s+");
                } /* else, we have <tag attrName > */
                ParseSuccessful(attr);
                node.Attributes.Add(attr);
            }
            node.Attributes.TrailingWhitespace = ws;
            if (rbracket == "/>")
            {
                node.TagStyle = TagStyle.SelfClosing;
                return ParseSuccessful(node);
            }
            else if (options.SelfClosingOnlyTagsSet.Contains(tagName))
            {
                node.TagStyle = TagStyle.CompactSelfClosing;
                return ParseSuccessful(node);
            }
            // TAG content
            if (ParseUntilClosingTag(node))
                return ParseSuccessful(node);
            return ParseFailed<TagNode>();
        }

        private bool ParseUntilClosingTag(TagNode tag)
        {
            var normalizedTagName = tag.Name.ToLowerInvariant();
            Regex matcher;
            var closingTagExpr = "(?i)</(" + Regex.Escape(normalizedTagName) + @")(\s*)>";
            lock (closingTagMatcherCache)
            {
                matcher = closingTagMatcherCache.TryGetValue(normalizedTagName);
                if (matcher == null)
                {
                    matcher = new Regex(closingTagExpr);
                    closingTagMatcherCache.Add(normalizedTagName, matcher);
                }
            }
            Match closingTagMatch;
            if (tag is ParserTag pt)
            {
                // For parser tags, we just read to the end.
                closingTagMatch = matcher.Match(fulltext, position);
                if (closingTagMatch.Success)
                {
                    pt.Content = fulltext.Substring(position, closingTagMatch.Index - position);
                    MovePositionTo(closingTagMatch.Index + closingTagMatch.Length);
                    goto CLOSE_TAG;
                }
                // If the parser tag doesn't close, then we fail. Pity.
                return false;
            }
            // We'll parse into the innerHTML of HTML tag.
            var ht = (HtmlTag)tag;
            if (normalizedTagName == "li")
            {
                // LI_TAG
                // <li> can be closed by </li>, <li ...> or EOF
                // <li> tags closed by <li ...>, or EOF are using TagStyle.NotClosed .
                ParseStart(@"</li\s*>|<li(\s*>|\s+)", false);
            }
            else
            {
                // OTHER_TAG
                ParseStart(closingTagExpr, false);
            }
            ht.Content = ParseWikitext();
            Accept();
            // Try to consume the tag closing.
            var closingTag = ConsumeToken(closingTagExpr);
            if (closingTag == null)
            {
                // Unbalanced HTML tag. This is a pathological case.
                // We won't backtrack here, as this is expensive and MW forces to close tags at EOF.
                tag.SetInferredClosingMark();
                tag.ClosingTagName = null;
                tag.TagStyle = TagStyle.NotClosed;
                return true;
            }
            closingTagMatch = matcher.Match(closingTag);
        CLOSE_TAG:
            Debug.Assert(closingTagMatch.Success);
            Debug.Assert(closingTagMatch.Groups[1].Success);
            Debug.Assert(closingTagMatch.Groups[2].Success);
            tag.ClosingTagName = closingTagMatch.Groups[1].Value != tag.Name
                ? closingTagMatch.Groups[1].Value
                : null;
            tag.ClosingTagTrailingWhitespace = closingTagMatch.Groups[2].Value;
            return true;
        }

        private Run ParseAttributeName()
        {
            ParseStart(@"/?>|[\s=]", true);
            var node = new Run();
            if (ParseRun(RunParsingMode.Run, node, false))
                return ParseSuccessful(node);
            return ParseFailed<Run>();
        }

        private Wikitext ParseAttributeValue(ValueQuoteType quoteType)
        {
            Wikitext node;
            ParseStart(null, true);
            switch (quoteType)
            {
                case ValueQuoteType.None:
                    CurrentContext.Terminator = Terminator.Get(@"[>\s]|/>");
                    node = ParseWikitext();
                    return ParseSuccessful(node, false);
                case ValueQuoteType.SingleQuotes:
                    if (ConsumeToken("\'") != null)
                    {
                        // Still, no right angle brackets are allowed
                        CurrentContext.Terminator = Terminator.Get("[>\']|/>");
                        node = ParseWikitext();
                        if (ConsumeToken("\'(?=\\s|>)") != null)
                            return ParseSuccessful(node, false);
                        // Otherwise, we're facing something like
                        // <tag attr='value'value>
                        // Treat it as unquoted text.
                    }
                    break;
                case ValueQuoteType.DoubleQuotes:
                    if (ConsumeToken("\"") != null)
                    {
                        // Still, no right angle brackets are allowed
                        CurrentContext.Terminator = Terminator.Get("[>\"]|/>");
                        node = ParseWikitext();
                        if (ConsumeToken("\"(?=\\s|>)") != null)
                            return ParseSuccessful(node, false);
                    }
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }
            return ParseFailed<Wikitext>();
        }
    }
}