using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MwParserFromScratch.Nodes;

namespace MwParserFromScratch
{
    /// <summary>
    /// A parser that parses Wikitext into AST.
    /// </summary>
    public class WikitextParser
    {
        private string fulltext;
        private int position; // Starting index of the string to be consumed.
        private int lineNumber, linePosition;
        private Stack<ParsingContext> contextStack;

        /// <summary>
        /// Parses the specified Wikitext.
        /// </summary>
        /// <param name="wikitext">The wikitext to be parsed.</param>
        /// <returns>A <see cref="Wikitext"/> node containing the AST of the given Wikitext.</returns>
        public Wikitext Parse(string wikitext)
        {
            if (wikitext == null) throw new ArgumentNullException(nameof(wikitext));
            // Initialize
            this.fulltext = wikitext;
            lineNumber = linePosition = 1;
            position = 0;
            contextStack = new Stack<ParsingContext>();
            // Then parse
            var root = ParseWikitext();
            Debug.Assert(position == fulltext.Length,
                "Detected unparsed wikitext after parsing. There might be a bug with parser.");
            Debug.Assert(contextStack.Count == 0,
                "Detected remaining ParsingContext on context stack. There might be a bug with parser.");
            // Cleanup
            fulltext = null;
            contextStack = null;
            return root;
        }

        /// <summary>
        /// Looks ahead and checks whether to terminate the current matching.
        /// </summary>
        private bool NeedsTerminate(Terminator ignoredTerminator = null)
        {
            if (position >= fulltext.Length) return true;
            foreach (var context in contextStack)
            {
                if (context.Terminator != ignoredTerminator && context.IsTerminated(fulltext, position))
                    return true;
                if (context.OverridesTerminator) break;
            }
            return false;
        }

        /// <summary>
        /// Looks ahead and checks where to terminate the PLAIN_TEXT matching.
        /// </summary>
        /// <returns>The index of the first character of the terminator.</returns>
        private int FindTerminator()
        {
            if (position >= fulltext.Length) return -1;
            int index = -1;
            foreach (var context in contextStack)
            {
                var newIndex = context.FindTerminator(fulltext, position);
                if (newIndex >= 0 && (index < 0 || newIndex < index))
                    index = newIndex;
            }
            return index;
        }

        private void ParseStart()
        {
            ParseStart(null, false);
        }

        private void ParseStart(string terminatorExpr, bool overridesTerminator)
        {
            var context = new ParsingContext(terminatorExpr == null ? null : Terminator.Get(terminatorExpr),
                overridesTerminator, position, lineNumber, linePosition);
            contextStack.Push(context);
        }

        private ParsingContext CurrentContext => contextStack.Peek();

        private T ParseSuccessful<T>(T value, bool setLineNumber = true) where T : Node
        {
            Debug.Assert(value != null);
            if (setLineNumber) value.SetLineInfo(CurrentContext.StartingLineNumber, CurrentContext.StartingLinePosition);
            Accept();
            return value;
        }

        private bool Accept()
        {
            contextStack.Pop();
            return true;
        }

        private T ParseFailed<T>(T node = default(T)) where T : Node
        {
            Fallback();
            return default(T);
        }

        private bool Fallback()
        {
            var context = contextStack.Pop();
            // Fallback
            position = context.StartingPosition;
            lineNumber = context.StartingLineNumber;
            linePosition = context.StartingLinePosition;
            return false;
        }

        /// <summary>
        /// WIKITEXT
        /// </summary>
        /// <remarks>The empty wikitext contains nothing. Thus the parsing should always be successful.</remarks>
        private Wikitext ParseWikitext()
        {
            ParseStart();
            var node = new Wikitext();
            Paragraph unclosedParagraph = null;
            while (!NeedsTerminate())
            {
                LineNode extraNode;
                var line = ParseLine(ref unclosedParagraph, out extraNode);
                Debug.Assert(extraNode != null, "extraNode shouldn't be null, but it can be <EmptyLineNode>.");
                if (line == null) break;
                if (line != EmptyLineNode) node.Lines.Add(line);
                if (extraNode != EmptyLineNode) node.Lines.Add(extraNode);
                if (ConsumeToken(@"\n") == null) break;
            }
            return ParseSuccessful(node);
        }

        /// <summary>
        /// Indicates the parsing is successful, but no node should be inserted to the list.
        /// </summary>
        private static readonly LineNode EmptyLineNode = new Paragraph(new PlainText("--EmptyLineNode--"));

        /// <summary>
        /// LINE
        /// </summary>
        /// <remarks>The parsing will always succeed.</remarks>
        private LineNode ParseLine(ref Paragraph unclosedParagraph, out LineNode extraNode)
        {
            // We use unclosedParagraph to handle breaks in paragraphs more easily.
            // This function may return more than one node; that's why extraNode exists.
            extraNode = EmptyLineNode;
            ParseStart(@"\n", false);
            LineNode node;
            // LIST_ITEM / HEADING automatically closes the last PARAGRAPH
            if ((node = ParseListItem()) != null)
            {
                unclosedParagraph = null;
                return ParseSuccessful(node);
            }
            if ((node = ParseHeading()) != null)
            {
                unclosedParagraph = null;
                return ParseSuccessful(node);
            }
            Paragraph para;
            if ((para = ParseCompactParagraph(unclosedParagraph)) != null)
            {
                var extraPara = ParseParagraphClose(para);
                if (extraPara == null)
                {
                    // The paragraph is not closed, and more content incoming.
                    if (unclosedParagraph == null)
                    {
                        unclosedParagraph = para;
                        return ParseSuccessful(para);
                    }
                    Debug.Assert(unclosedParagraph == para);
                    return ParseSuccessful(EmptyLineNode, false);
                }
                else
                {
                    // The paragraph is closed.
                    extraNode = extraPara;
                    if (unclosedParagraph == null) return ParseSuccessful(para);
                    Debug.Assert(unclosedParagraph == para);
                    return ParseSuccessful(EmptyLineNode, false);
                }
            }
            // Note ParseParagraph will always succeed.
            Debug.Assert(false);
            return ParseFailed<LineNode>();
        }

        /// <summary>
        /// Parses a PARAGRPAH_CLOSE .
        /// </summary>
        /// <param name="lastParagraph">The lastest parsed paragrpah.</param>
        /// <returns>The extra paragraph, or <see cref="EmptyLineNode"/>. If the parsing attmpt failed, <c>null</c>.</returns>
        private LineNode ParseParagraphClose(Paragraph lastParagraph)
        {
            Debug.Assert(lastParagraph != null);
            Debug.Assert(lastParagraph.Compact);
            ParseStart();
            // 2 line breaks (\n\n) or \n Terminator closes the paragraph,
            // so do a look-ahead here. Note that a \n will be consumed in ParseWikitext .
            // Note that this look-ahead will also bypass the \n terminator defined in WIKITEXT

            // For the last paragraph
            // TERM     Terminators
            // PC       Compact/unclosed paragraph
            // P        Closed paragraph
            // abc TERM     PC[|abc|]
            // abc\n TERM   P[|abc|]
            // abc\n\n TERM PC[|abc|]PC[||]
            // Note that MediaWiki editor will automatically trim the trailing whitespaces,
            // leaving a \n after the content. This one \n will be removed when the page is transcluded.
            if (ConsumeToken(@"\n") != null)
            {
                // Already consumed a \n, attempt to consume another \n
                ParseStart();
                if (ConsumeToken(@"\n") != null)
                {
                    // abc\n\n TERM PC[|abc|]PC[||]
                    // Note here TERM excludes \n
                    if (NeedsTerminate(Terminator.Get(@"\n")))
                    {
                        Debug.Assert(lastParagraph.Compact);
                        // Might as well consume 2 \n here. Leave the TERM to WIKITEXT parser.
                        Accept();
                        // Note that we already had lastParagraph.Compact == true
                        return ParseSuccessful(new Paragraph {Compact = true});
                    }
                    // More content incoming.
                    // abc\n\n def
                    // Consuem a \n to closed the current paragraph. Leave a \n to WIKITEXT parser.
                    lastParagraph.Compact = false;
                    Fallback();
                    return ParseSuccessful(EmptyLineNode, false);
                }
                // The attempt to consume the 2nd \n failed. Fallback first.
                // This won't change the position.
                Fallback();
                if (NeedsTerminate())
                {
                    // abc\n TERM   P[|abc|]
                    lastParagraph.Compact = false;
                    return ParseSuccessful(EmptyLineNode, false);
                }
            }
            // abc \n def
            // That's not the end of a prargraph.
            return ParseFailed<LineNode>();
        }

        /// <summary>
        /// LIST_ITEM
        /// </summary>
        private ListItem ParseListItem()
        {
            ParseStart();
            var prefix = ConsumeToken("[*#:;]+|-{4,}| ");
            if (prefix == null) return ParseFailed<ListItem>();
            var node = new ListItem {Prefix = prefix};
            ParseRun(RunParsingMode.Run, node); // optional
            return ParseSuccessful(node);
        }

        /// <summary>
        /// HEADING
        /// </summary>
        private Heading ParseHeading()
        {
            // Look ahead to determine the maximum level, assuming the line is a valid heading.
            var prefix = LookAheadToken("={1,6}");
            if (prefix == null) return null;

            // Note that here we require all the headings terminate with \n or EOF, so this won't be recognized
            // {{{ARG|== Default Heading ==}}}\n
            // But this will work
            // {{{ARG|== Default Heading ==
            // }}}
            // Note that this should be recognized as heading
            //  == {{T|
            //  arg1}} ==

            // Test different levels of heading
            var node = new Heading();
            for (var level = prefix.Length; level > 0; level--)
            {
                var prefixExpr = "={" + level + "}";
                var suffixExpr = "(?m)={" + level + "}$";
                ParseStart(suffixExpr, false);
                ConsumeToken(prefixExpr);
                if (!ParseRun(RunParsingMode.Run, node))
                {
                    ParseFailed<Heading>();
                    continue;
                }
                if (ConsumeToken(suffixExpr) == null)
                {
                    ParseFailed<Heading>();
                    continue;
                }
                node.Level = level;
                return ParseSuccessful(node);
            }
            // Failed (E.g. ^=== Title )
            return null;
        }

        /// <summary>
        /// PARAGRAPH
        /// </summary>
        /// <remarks>The parsing operation will always succeed.</remarks>
        private Paragraph ParseCompactParagraph(Paragraph mergeTo)
        {
            Debug.Assert(mergeTo == null || mergeTo.Compact);
            // Create a new paragraph, or merge the new line to the existing paragraph.
            ParseStart();
            var node = mergeTo;
            mergeTo?.Append("\n");
            if (node == null)
            {
                node = new Paragraph {Compact = true};
            }
            // Allows an empty paragraph. (content == null)
            ParseRun(RunParsingMode.Run, node);
            return ParseSuccessful(node, mergeTo == null);
        }

        /// <summary>
        /// RUN
        /// </summary>
        private bool ParseRun(RunParsingMode mode, InlineContainer container)
        {
            ParseStart();
            var parsedAny = false;
            while (!NeedsTerminate())
            {
                // Read more
                InlineNode inline;
                if ((inline = ParseExpandable()) != null) goto NEXT;
                switch (mode)
                {
                    case RunParsingMode.Run: // RUN
                        if ((inline = ParseInline()) != null) goto NEXT;
                        break;
                    case RunParsingMode.ExpandableText: // EXPANDABLE_TEXT
                        if ((inline = ParsePartialPlainText()) != null) goto NEXT;
                        break;
                    case RunParsingMode.ExpandableUrl: // EXPANDABLE_URL
                        if ((inline = ParseUrlText()) != null) goto NEXT;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
                }
                break;
                NEXT:
                parsedAny = true;
                // Remember that ParsePartialText stops whenever there's a susceptable termination of PLAIN_TEXT
                // So we need to marge the consequent PlainText objects.
                var newtext = inline as PlainText;
                if (newtext != null)
                {
                    var lastText = container.Inlines.LastNode as PlainText;
                    if (lastText != null)
                    {
                        lastText.Content += newtext.Content;
                        continue;
                    }
                }
                container.Inlines.Add(inline);
            }
            // Note that the content of RUN should not be empty.
            return parsedAny ? Accept() : Fallback();
        }

        private InlineNode ParseInline()
        {
            ParseStart();
            InlineNode node;
            if ((node = ParseWikiLink()) != null) return ParseSuccessful(node);
            if ((node = ParseExternalLink()) != null) return ParseSuccessful(node);
            if ((node = ParseFormatSwitch()) != null) return ParseSuccessful(node);
            if ((node = ParsePartialPlainText()) != null) return ParseSuccessful(node);
            return ParseFailed<InlineNode>();
        }

        private InlineNode ParseExpandable()
        {
            ParseStart();
            InlineNode node;
            if ((node = ParseComment()) != null) return ParseSuccessful(node);
            return ParseFailed<InlineNode>();
        }

        private WikiLink ParseWikiLink()
        {
            ParseStart(@"\||\]\]", false);
            if (ConsumeToken(@"\[\[") == null) return ParseFailed<WikiLink>();
            var target = new Run();
            if (!ParseRun(RunParsingMode.ExpandableText, target)) return ParseFailed<WikiLink>();
            var node = new WikiLink {Target = target};
            if (ConsumeToken(@"\|") != null)
            {
                var text = new Run();
                // Text accepts pipe
                CurrentContext.Terminator = Terminator.Get(@"\]\]");
                // For [[target|]], Text == Empty Run
                // For [[target]], Text == null
                if (ParseRun(RunParsingMode.ExpandableText, text))
                    node.Text = text;
            }
            if (ConsumeToken(@"\]\]") == null) return ParseFailed<WikiLink>();
            return ParseSuccessful(node);
        }

        private ExternalLink ParseExternalLink()
        {
            ParseStart(@"[\s\]]", false);
            var brackets = ConsumeToken(@"\[") != null;
            // Parse target
            Run target;
            if (brackets)
            {
                target = new Run();
                // Aggressive
                ParseRun(RunParsingMode.ExpandableUrl, target);
            }
            else
            {
                // Conservative
                var url = ParseUrlText();
                target = url == null ? null : new Run(url);
            }
            if (target == null) return ParseFailed<ExternalLink>();
            var node = new ExternalLink {Target = target, Brackets = brackets};
            if (brackets)
            {
                // Parse text
                if (ConsumeToken(@"[ \t]") != null)
                {
                    CurrentContext.Terminator = Terminator.Get(@"\]");
                    var text = new Run();
                    // For [http://target  ], Text == " "
                    // For [http://target ], Text == Empty Run
                    // For [http://target], Text == null
                    if (ParseRun(RunParsingMode.Run, text))
                        node.Text = text;
                }
                if (ConsumeToken(@"\]") == null) return ParseFailed(node);
            }
            return ParseSuccessful(node);
        }

        private FormatSwitch ParseFormatSwitch()
        {
            // For 4 or 5+ quotes, discard quotes on the left.
            var token = ConsumeToken("('{5}|'''|'')(?!')");
            if (token == null) return null;
            switch (token.Length)
            {
                case 2:
                    return new FormatSwitch(false, true);
                case 3:
                    return new FormatSwitch(true, false);
                case 5:
                    return new FormatSwitch(true, true);
                default:
                    Debug.Assert(false);
                    return null;
            }
        }

        private static readonly Regex CommentSuffixMatcher = new Regex("-->");

        private Comment ParseComment()
        {
            ParseStart();
            if (ConsumeToken("<!--") == null) return ParseFailed<Comment>();
            var contentPos = position;
            var suffix = CommentSuffixMatcher.Match(fulltext, position);
            if (suffix.Success)
            {
                MovePositionTo(suffix.Index + suffix.Length);
                return ParseSuccessful(new Comment(fulltext.Substring(contentPos, suffix.Index - contentPos)));
            }
            MovePositionTo(fulltext.Length);
            return ParseSuccessful(new Comment(fulltext.Substring(contentPos)));
        }

        private void MovePositionTo(int newPosition)
        {
            if (newPosition <= position) throw new ArgumentOutOfRangeException(nameof(newPosition));
            // The position indicates the beginning of the next token.
            for (var i = position; i < newPosition; i++)
            {
                if (fulltext[i] == '\n')
                {
                    lineNumber++;
                    linePosition = 1;
                }
                else
                {
                    linePosition++;
                }
            }
            position = newPosition;
        }

        // Used to stop plain text parsing in case there's a starting of other elements.
        // Don't forget there's already a terminator \n in the LINE derivation
        private static readonly Regex SuspectablePlainTextEndMatcher =
            new Regex(@"\[|\{\{\{?
                        |<(\s*\w|!--)
                        |('{5}|'''|'')(?!')
                        |(((\bhttps?:|\bftp:|\birc:|\bgopher:|)\/\/)|\bnews:|\bmailto:)",
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        /// <summary>
        /// PLAIN_TEXT
        /// </summary>
        /// <remarks>A PLAIN_TEXT contains at least 1 character.</remarks>
        private PlainText ParsePartialPlainText()
        {
            // Terminates now?
            if (NeedsTerminate()) return null;
            // Or find the nearest terminator.
            var terminatorPos = FindTerminator();
            if (terminatorPos < 0) terminatorPos = fulltext.Length;
            // We'll at least consume 1 character.
            var susceptableEnd = SuspectablePlainTextEndMatcher.Match(fulltext, position + 1,
                terminatorPos - position - 1);
            var origPos = position;
            if (susceptableEnd.Success)
            {
                MovePositionTo(susceptableEnd.Index);
                return new PlainText(fulltext.Substring(origPos, susceptableEnd.Index - origPos));
            }
            else if (terminatorPos > 0)
            {
                MovePositionTo(terminatorPos);
                return new PlainText(fulltext.Substring(origPos, terminatorPos - origPos));
            }
            else
            {
                MovePositionTo(fulltext.Length);
                return new PlainText(fulltext.Substring(origPos));
            }
        }

        private static readonly Regex UrlMatcher =
            new Regex(
                @"(?i)(((\bhttps?:|\bftp:|\birc:|\bgopher:|)\/\/)|\bnews:|\bmailto:)([^\x00-\x20\s""\[\]\x7f\|\{\}<>]|<[^>]*>)+?(?=([!""().,:;‘-•]*\s|[\x00-\x20\s""\[\]\x7f|{}]|$))");

        private PlainText ParseUrlText()
        {
            // First, find an appearant terminator of URL
            var endPos = position;
            for (; endPos < fulltext.Length; endPos++)
                if (char.IsWhiteSpace(fulltext, endPos)) break;
            // From https://en.wikipedia.org/wiki/User:Cacycle/wikEd.js
            var match = UrlMatcher.Match(fulltext, position, endPos - position);
            // We only take the URL if it starts IMMEDIATELY.
            if (match.Success && match.Index == position)
            {
                MovePositionTo(position + match.Length);
                return new PlainText(match.Value);
            }
            return null;
        }

        private static Dictionary<string, Regex> tokenMatcherCache = new Dictionary<string, Regex>();

        /// <summary>
        /// Match the next token. This operation will not consume the tokens.
        /// </summary>
        /// <param name="tokenMatcher">A regular expression string used to match the next token.</param>
        /// <returns>The string of token that has been successfully matched. OR <c>null</c> is such attempt failed.</returns>
        private string LookAheadToken(string tokenMatcher)
        {
            Debug.Assert(tokenMatcher[0] != '^');
            var re = tokenMatcherCache.TryGetValue(tokenMatcher);
            if (re == null)
            {
                // The occurance should be starting from current position.
                re = new Regex(@"\G(" + tokenMatcher + ")");
                tokenMatcherCache.Add(tokenMatcher, re);
            }
            var m = re.Match(fulltext, position);
            if (!m.Success) return null;
            Debug.Assert(position == m.Index);
            // We want to move forward.
            Debug.Assert(m.Length > 0);
            return m.Value;
        }

        /// <summary>
        /// Attempts to consume the next token.
        /// </summary>
        /// <param name="tokenMatcher">A regular expression string used to match the next token.</param>
        /// <returns>The string of token that has been successfully consumed. OR <c>null</c> is such attempt failed.</returns>
        private string ConsumeToken(string tokenMatcher)
        {
            var t = LookAheadToken(tokenMatcher);
            if (t == null) return null;
            // The position indicates the beginning of the next token.
            MovePositionTo(position + t.Length);
            return t;
        }

        private enum RunParsingMode
        {
            Run = 0,
            ExpandableText = 1,
            ExpandableUrl = 2,
        }

        private class ParsingContext
        {
            public Terminator Terminator { get; set; }

            /// <summary>
            /// Whether to stop looking for terminators to the bottom of the stack.
            /// </summary>
            public bool OverridesTerminator { get; }

            public int StartingPosition { get; }

            public int StartingLineNumber { get; }

            public int StartingLinePosition { get; }

            public bool IsTerminated(string str, int startIndex)
            {
                Debug.Assert(str != null);
                if (Terminator == null) return false;
                return Terminator.IsTerminated(str, startIndex);
            }

            public int FindTerminator(string str, int startIndex)
            {
                Debug.Assert(str != null);
                if (Terminator == null) return - 1;
                return Terminator.Search(str, startIndex);
            }

            /// <summary>
            /// 返回表示当前对象的字符串。
            /// </summary>
            /// <returns>
            /// 表示当前对象的字符串。
            /// </returns>
            public override string ToString() => $"({StartingLineNumber},{StartingLinePosition})/{Terminator}/";

            public ParsingContext(Terminator terminator, bool overridesTerminator, int startingPosition,
                int startingLineNumber, int startingLinePosition)
            {
                Terminator = terminator;
                OverridesTerminator = overridesTerminator;
                StartingPosition = startingPosition;
                StartingLineNumber = startingLineNumber;
                StartingLinePosition = startingLinePosition;
            }
        }

        private class Terminator
        {
            private static readonly Dictionary<string, Terminator> cacheDict = new Dictionary<string, Terminator>();

            private readonly Regex matcher;
            private readonly Regex searcher;

            public bool IsTerminated(string str, int startIndex)
            {
                return matcher.IsMatch(str, startIndex);
            }

            /// <summary>
            /// Search for the index of the beginning of the terminator.
            /// </summary>
            /// <returns>Index of the beginning of the terminator. OR -1 if no such terminator is found.</returns>
            public int Search(string str, int startIndex)
            {
                var match = searcher.Match(str, startIndex);
                return match.Success ? match.Index : -1;
            }

            private Terminator(string expr)
            {
                Debug.Assert(expr[0] != '^');
                matcher = new Regex(@"\G(" + expr + ")");
                searcher = new Regex(expr);
            }

            public override string ToString() => searcher.ToString();

            public static Terminator Get(string regularExpr)
            {
                var t = cacheDict.TryGetValue(regularExpr);
                if (t == null)
                {
                    t = new Terminator(regularExpr);
                    cacheDict.Add(regularExpr, t);
                }
                return t;
            }
        }
    }
}
