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
        private int position;        // Starting index of the string to be consumed.
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
            Debug.Assert(position == fulltext.Length, "Detected unparsed wikitext after parsing. There might be a bug with parser.");
            // Cleanup
            fulltext = null;
            contextStack = null;
            return root;
        }

        /// <summary>
        /// Looks ahead and checks whether to terminate the current matching.
        /// </summary>
        private bool NeedsTerminate()
        {
            if (position >= fulltext.Length) return true;
            Terminator removedTerminator = null;
            foreach (var context in contextStack)
            {
                if (context.RemovedTerminator != null)
                {
                    Debug.Assert(removedTerminator == null, "You need a HashSet to contain removedTerminators. (This is likely a bug in parsing, because usually the assertion will pass.)");
                    removedTerminator = context.RemovedTerminator;
                }
                if (context.Terminator != removedTerminator && context.IsTerminated(fulltext, position))
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
            Terminator removedTerminator = null;
            int index = -1;
            foreach (var context in contextStack)
            {
                if (context.RemovedTerminator != null)
                {
                    Debug.Assert(removedTerminator == null,
                        "You need a HashSet to contain removedTerminators. (This is likely a bug in parsing, because usually the assertion will pass.)");
                    removedTerminator = context.RemovedTerminator;
                }
                if (context.Terminator != removedTerminator)
                {
                    var newIndex = context.FindTerminator(fulltext, position);
                    if (newIndex >= 0 && (index < 0 || newIndex < index))
                        index = newIndex;
                }
            }
            return index;
        }

        private void ParseStart()
        {
            ParseStart(null, null, false);
        }

        private void ParseStart(string terminatorExpr, bool overridesTerminator)
        {
            ParseStart(terminatorExpr, null, overridesTerminator);
        }

        private void ParseStart(string terminatorExpr, string removedTerminatorExpr)
        {
            ParseStart(terminatorExpr, removedTerminatorExpr, false);
        }

        private void ParseStart(string terminatorExpr, string removedTerminatorExpr, bool overridesTerminator)
        {
            var context = new ParsingContext(terminatorExpr, removedTerminatorExpr, overridesTerminator,
                position, lineNumber, linePosition);
            contextStack.Push(context);
        }

        private T ParseSuccessful<T>(T value, bool setLineNumber = true) where T : Node
        {
            Debug.Assert(value != null);
            var context = contextStack.Pop();
            if (setLineNumber) value.SetLineInfo(context.StartingLineNumber, context.StartingLinePosition);
            return value;
        }

        private T ParseFailed<T>(T node = default(T)) where T : Node
        {
            var context = contextStack.Pop();
            // Fallback
            position = context.StartingPosition;
            lineNumber = context.StartingLineNumber;
            linePosition = context.StartingLinePosition;
            return default(T);
        }

        /// <summary>
        /// WIKITEXT
        /// </summary>
        /// <remarks>The empty wikitext contains nothing. Thus the parsing should always be successful.</remarks>
        private Wikitext ParseWikitext()
        {
            ParseStart();
            var node = new Wikitext();
            Paragraph lastParagraph = null;
            while (!NeedsTerminate())
            {
                var line = ParseLine(ref lastParagraph);
                if (line == null) break;
                if (line != EmptyLineNode)
                {
                    node.Lines.Add(line);
                }
                if (ConsumeToken(@"\n") == null) break;
            }
            return ParseSuccessful(node);
        }

        /// <summary>
        /// Indicates the parsing is successful, but no node should be inserted to the list.
        /// </summary>
        private static readonly LineNode EmptyLineNode = new Paragraph();

        /// <summary>
        /// LINE
        /// </summary>
        /// <remarks>The parsing will always succeed.</remarks>
        private LineNode ParseLine(ref Paragraph unclosedParagraph)
        {
            // We use lastLine to handle breaks in paragraphs more easily.
            ParseStart(@"\n", false);
            LineNode node;
            // LIST_ITEM / HEADING automatically close PARAGRAPH
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
            // 2 line breaks closes the paragraph
            if (unclosedParagraph != null && ConsumeToken(@"\n") != null)
            {
                unclosedParagraph.Compact = false;
                unclosedParagraph = null;
                return ParseSuccessful(EmptyLineNode, false);
            }
            if ((node = unclosedParagraph = ParseParagraph(unclosedParagraph)) != null)
            {
                // while 1 line break might only be displayed as a space in the paragraph
                return ParseSuccessful(node);
            }
            // Note ParseParagraph will always succeed.
            Debug.Assert(node != null);
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
            var content = ParseRun(RunParsingMode.Run);   // optional
            return ParseSuccessful(new ListItem {Prefix = prefix, Content = content});
        }

        private static readonly Regex HeadingPrefixMatcher = new Regex("^={1,6}");
        private static readonly Regex HeadingSuffixMatcher = new Regex("={1,6}$", RegexOptions.Multiline);

        /// <summary>
        /// HEADING
        /// </summary>
        private Heading ParseHeading()
        {
            // Look ahead to determine the level, if the line is a valid heading.
            var prefix = HeadingPrefixMatcher.Match(fulltext, position);
            if (!prefix.Success) return null;
            var suffix = HeadingSuffixMatcher.Match(fulltext, position);
            if (!suffix.Success) return null;
            var bar = prefix.Length > suffix.Length ? suffix.Value : prefix.Value;
            // TODO Handle the following case
            // ====== , which is the same as == == ==
            // This is where the real parsing starts.
            ParseStart(bar + "(?m)$", true);
            var token = ConsumeToken(bar);
            Debug.Assert(token != null);

            var content = ParseRun(RunParsingMode.Run);
            if (content == null) return ParseFailed<Heading>();

            token = ConsumeToken(bar);
            if (token == null) return ParseFailed<Heading>();

            return ParseSuccessful(new Heading {Level = bar.Length, Title = content});
        }

        /// <summary>
        /// PARAGRAPH
        /// </summary>
        /// <remarks>The parsing operation will always succeed.</remarks>
        private Paragraph ParseParagraph(Paragraph mergeTo)
        {
            // Create a new paragraph, or merge the new line to the existing paragraph.
            ParseStart();
            mergeTo?.Append("\n");
            // Allows empty line
            if (NeedsTerminate())
                return ParseSuccessful(mergeTo ?? new Paragraph {Compact = true}, mergeTo == null);
            // An empty paragraph is a paragraph with no Run
            var run = ParseRun(RunParsingMode.Run);
            mergeTo?.Append(run);
            return ParseSuccessful(new Paragraph {Content = run, Compact = true}, mergeTo == null);
        }

        /// <summary>
        /// RUN
        /// </summary>
        private Run ParseRun(RunParsingMode mode)
        {
            ParseStart();
            Run node = null;
            while (!NeedsTerminate())
            {
                // Read more
                InlineNode inline;
                if ((inline = ParseExpandable()) != null) goto NEXT;
                switch (mode)
                {
                    case RunParsingMode.Run:                // RUN
                        if ((inline = ParseInline()) != null) goto NEXT;
                        break;
                    case RunParsingMode.ExpandableText:     // EXPANDABLE_TEXT
                        if ((inline = ParsePartialPlainText()) != null) goto NEXT;
                        break;
                    case RunParsingMode.ExpandableUrl:      // EXPANDABLE_URL
                        if ((inline = ParseUrlText()) != null) goto NEXT;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
                }
                break;
                NEXT:
                if (node == null) node = new Run();
                // Remember that ParsePartialText stops whenever there's a susceptable termination of PLAIN_TEXT
                // So we need to marge the consequent PlainText objects.
                var newtext = inline as PlainText;
                if (newtext != null)
                {
                    var lastText = node.Inlines.LastNode as PlainText;
                    if (lastText != null)
                    {
                        lastText.Content += newtext.Content;
                        continue;
                    }
                }
                node.Inlines.Add(inline);
            }
            // Note that the content of RUN should not be empty.
            return node == null ? ParseFailed<Run>() : ParseSuccessful(node);
        }

        private InlineNode ParseInline()
        {
            ParseStart();
            InlineNode node;
            if ((node = ParseWikiLink()) != null) return ParseSuccessful(node);
            if ((node = ParseExternalLink()) != null) return ParseSuccessful(node);
            if ((node = ParseSimpleFormat()) != null) return ParseSuccessful(node);
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
            ParseStart(@"\]\]|\n", false);
            if (ConsumeToken(@"\[\[") == null) return ParseFailed<WikiLink>();
            var node = new WikiLink();
            node.Target = ParseRun(RunParsingMode.ExpandableText);
            if (node.Target == null) return ParseFailed<WikiLink>();
            if (ConsumeToken(@"\|") != null)
            {
                node.Text = ParseRun(RunParsingMode.ExpandableText);
            }
            if (ConsumeToken(@"\]\]") == null) return ParseFailed<WikiLink>();
            return ParseSuccessful(node);
        }

        private ExternalLink ParseExternalLink()
        {
            ParseStart(@"\]|\n", false);
            var brackets = ConsumeToken(@"\[") != null;
            Run target;
            if (brackets)
            {
                // Aggressive
                target = ParseRun(RunParsingMode.ExpandableUrl);
            }
            else
            {
                // Conservative
                var url = ParseUrlText();
                target = url == null ? null : new Run(url);
            }
            if (target == null) return ParseFailed<ExternalLink>();
            var node = new ExternalLink {Target = target};
            if (node.Target == null) return ParseFailed(node);
            if (ConsumeToken(@"[ \t]") != null)
            {
                node.Text = ParseRun(RunParsingMode.Run);
            }
            if (ConsumeToken(@"\]") == null) return ParseFailed(node);
            return ParseSuccessful(node);
        }

        private SimpleFormat ParseSimpleFormat()
        {
            var prefix = "'''";
            PARSE:
            ParseStart(prefix, false);
            var token = ConsumeToken(prefix);
            if (token == null) goto FAIL;
            var content = ParseRun(RunParsingMode.Run);
            if (content == null) goto FAIL;
            var suffix = ConsumeToken(token);
            // suffix != token often indicates the formatting tag is broken by new line
            // suffix can be null, as the formatting tag might be ended up with EOF
            return ParseSuccessful(new SimpleFormat {Content = content});
            FAIL:
            if (prefix == "'''")
            {
                prefix = "''";
                ParseFailed<SimpleFormat>();
                goto PARSE;
            }
            return ParseFailed<SimpleFormat>();
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

        // Don't forget there's already a terminator \n in the LINE derivation
        private static readonly Regex SuspectablePlainTextEndMatcher = new Regex(@"\[|<|{{{?|(?<=\s|^)(((\bhttps?:|\bftp:|\birc:|\bgopher:|)\/\/)|\bnews:|\bmailto:)", RegexOptions.IgnoreCase);

        /// <summary>
        /// PLAIN_TEXT
        /// </summary>
        private PlainText ParsePartialPlainText()
        {
            // Terminates now?
            if (NeedsTerminate()) return null;
            // Or find the nearest terminator.
            var terminatorPos = FindTerminator();
            if (terminatorPos < 0) terminatorPos = fulltext.Length;
            // We'll at least consume 1 character.
            var susceptableEnd = SuspectablePlainTextEndMatcher.Match(fulltext, position + 1, terminatorPos - position - 1);
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

        private PlainText ParseUrlText()
        {
            // From https://en.wikipedia.org/wiki/User:Cacycle/wikEd.js
            var token = ConsumeToken(@"(?i)(((\bhttps?:|\bftp:|\birc:|\bgopher:|)\/\/)|\bnews:|\bmailto:)([^\x00-\x20\s""\[\]\x7f\|\{\}<>]|<[^>]*>)+?(?=([!""().,:;‘-•]*\s|[\x00-\x20\s""\[\]\x7f|{}]|$))");
            if (token != null) return new PlainText(token);
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
                re = new Regex("^(" + tokenMatcher + ")");
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
            public Terminator Terminator { get; }

            public Terminator RemovedTerminator { get; }

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
                if (Terminator == null) return str.Length - 1;
                return Terminator.Search(str, startIndex);
            }

            /// <summary>
            /// 返回表示当前对象的字符串。
            /// </summary>
            /// <returns>
            /// 表示当前对象的字符串。
            /// </returns>
            public override string ToString() => $"({StartingLineNumber},{StartingLinePosition})/{Terminator}/";

            public ParsingContext(string terminatorExpr, string removedTerminatorExpr, bool overridesTerminator, int startingPosition, int startingLineNumber, int startingLinePosition)
            {
                Terminator = terminatorExpr == null ? null : Terminator.Get(terminatorExpr);
                RemovedTerminator = removedTerminatorExpr == null ? null : Terminator.Get(removedTerminatorExpr);
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
                matcher = new Regex("^" + expr);
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
