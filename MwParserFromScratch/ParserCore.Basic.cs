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
        /// <summary>
        /// WIKITEXT
        /// </summary>
        /// <remarks>An empty WIKITEXT contains nothing. Thus the parsing should always be successful.</remarks>
        private Wikitext ParseWikitext()
        {
            cancellationToken.ThrowIfCancellationRequested();
            ParseStart();
            var node = new Wikitext();
            LineNode lastLine = null;
            if (NeedsTerminate()) return ParseSuccessful(node);
            NEXT_LINE:
            var line = ParseLine(lastLine);
            if (line != EMPTY_LINE_NODE)
            {
                lastLine = line;
                node.Lines.Add(line);
            }
            var extraPara = ParseLineEnd(lastLine);
            if (extraPara == null)
            {
                // Failed to read a \n , which means we've reached a terminator.
                // This is guaranteed in ParseLineEnd
                Debug.Assert(NeedsTerminate());
                return ParseSuccessful(node);
            }
            // Otherwise, check whether we meet a terminator before reading another line.
            if (extraPara != EMPTY_LINE_NODE)
                node.Lines.Add(extraPara);
            if (NeedsTerminate()) return ParseSuccessful(node);
            goto NEXT_LINE;
        }

        /// <summary>
        /// Indicates the parsing is successful, but no node should be inserted to the list.
        /// </summary>
        private static readonly LineNode EMPTY_LINE_NODE = new Paragraph(new PlainText("--EmptyLineNode--"));

        /// <summary>
        /// Parses LINE.
        /// </summary>
        private LineNode ParseLine(LineNode lastLine)
        {
            ParseStart(@"\n", false);       // We want to set a terminator, so we need to call ParseStart
            // LIST_ITEM / HEADING automatically closes the previous PARAGRAPH
            var node = ParseListItem() ?? ParseHeading() ?? ParseCompactParagraph(lastLine);
            if (lastLine is IInlineContainer lastLineContainer)
            {
                if (lastLineContainer.Inlines.LastNode is PlainText pt && pt.Content.Length == 0)
                {
                    // This can happen because we appended a PlainText("") at (A) in ParseLineEnd
                    pt.Remove();
                }
            }
            if (node != null)
                Accept();
            else
                Fallback();
            return node;
        }

        /// <summary>
        /// Parses a PARAGRPAH_CLOSE .
        /// </summary>
        /// <param name="lastNode">The lastest parsed node.</param>
        /// <returns>The extra paragraph, or <see cref="EMPTY_LINE_NODE"/>. If parsing attempt failed, <c>null</c>.</returns>
        private LineNode ParseLineEnd(LineNode lastNode)
        {
            Debug.Assert(lastNode != null);
            var unclosedParagraph = lastNode as Paragraph;
            if (unclosedParagraph != null && !unclosedParagraph.Compact)
                unclosedParagraph = null;
            // 2 line breaks (\n\n) or \n Terminator closes the paragraph,
            // so do a look-ahead here. Note that a \n will be consumed in ParseWikitext .
            // Note that this look-ahead will also bypass the \n terminator defined in WIKITEXT

            // For the last non-empty line
            // TERM     Terminators
            // PC       Compact/unclosed paragraph
            // P        Closed paragraph
            // abc TERM             PC[|abc|]
            // abc\n TERM           P[|abc|]
            // abc\n\s*?\n TERM     P[|abc|]PC[||]
            // Note that MediaWiki editor will automatically trim the trailing whitespaces,
            // leaving a \n after the content. This one \n will be removed when the page is transcluded.
            var lastLinePosition = linePosition;
            // Here we consume a \n without fallback.
            if (ConsumeToken(@"\n") == null) return null;
            ParseStart();
            // Whitespaces between 2 \n, assuming there's a second \n or TERM after trailingWs
            var trailingWs = ConsumeToken(@"[\f\r\t\v\x85\p{Z}]+");
            if (unclosedParagraph != null)
            {
                // We're going to consume another \n or TERM to close the paragraph.
                // Already consumed a \n, attempt to consume another \n
                var trailingWsEndsAt = linePosition;
                if (ConsumeToken(@"\n") != null)
                {
                    // Close the last paragraph.
                    unclosedParagraph.AppendWithLineInfo("\n" + trailingWs,
                        // don't forget the position of leading '\n'
                        CurrentContext.StartingLineNumber - 1, lastLinePosition,
                        CurrentContext.StartingLineNumber, trailingWsEndsAt);
                    // 2 Line breaks received.
                    // Check for the special case. Note here TERM excludes \n
                    if (NeedsTerminate(Terminator.Get(@"\n")))
                    {
                        // This is a special case.
                        // abc \n trailingWs \n TERM --> P[|abc\ntrailingWs|]PC[||]
                        //                      ^ We are here.
                        // When the function returns, WIKITEXT parsing will stop
                        // because a TERM will be received.
                        // We need to correct this.
                        var anotherparagraph = new Paragraph();
                        anotherparagraph.SetLineInfo(lineNumber, linePosition, lineNumber, linePosition);
                        return ParseSuccessful(anotherparagraph, false);
                    }
                    // The last paragraph will be closed now.
                    return ParseSuccessful(EMPTY_LINE_NODE, false);
                }
                // The attempt to consume the 2nd \n failed.
                if (NeedsTerminate())
                {
                    // abc \n trailingWs TERM   P[|abc|]
                    //                   ^ We are here.
                    // If we need to terminate, then close the last paragraph.
                    unclosedParagraph.AppendWithLineInfo("\n" + trailingWs,
                        // don't forget the position of leading '\n'
                        CurrentContext.StartingLineNumber - 1, lastLinePosition,
                        lineNumber, linePosition);
                    return ParseSuccessful(EMPTY_LINE_NODE, false);
                }
                // The last paragraph is still not closed (i.e. compact paragraph).
                // (A)
                // Note here we have still consumed the first '\n', while the last paragraph has no trailing '\n'.
                // For continued PlainText, we will add a '\n' in ParseCompactParagraph.
                // Add an empty node so ParseCompactParagraph can add a '\n' with LineInfo.
                unclosedParagraph.AppendWithLineInfo("", CurrentContext.StartingLineNumber - 1, lastLinePosition,
                    CurrentContext.StartingLineNumber - 1, lastLinePosition);
                // Fallback so we can either continue parsing PlainText,
                // or discover the next, for example, Heading, and leave the last paragraph compact.
                Fallback();
                return EMPTY_LINE_NODE;
            }
            else
            {
                // Last node cannot be a closed paragraph.
                // It can't because ParseLineEnd is invoked immediately after a last node is parsed,
                // and only ParseLineEnd can close a paragraph.
                Debug.Assert(!(lastNode is Paragraph), "Last node cannot be a closed paragraph.");
                // Rather, last node is LINE node of other type (LIST_ITEM/HEADING).
                // Remember we've already consumed a '\n' , and the spaces after it.
                // The situation here is just like the "special case" mentioned above.
                if (NeedsTerminate(Terminator.Get(@"\n")))
                {
                    // abc \n WHITE_SPACE TERM  -->  [|abc|] PC[|WHITE_SPACE|]
                    //        ^ CurCntxt  ^ We are here now.
                    // Note here TERM excludes \n
                    var anotherparagraph = new Paragraph();
                    if (trailingWs != null)
                    {
                        var pt = new PlainText(trailingWs);
                        // Actually the same as what we do in ParseSuccessful for PlainText.
                        pt.SetLineInfo(CurrentContext.StartingLineNumber, CurrentContext.StartingLinePosition,
                            lineNumber, linePosition);
                        anotherparagraph.Inlines.Add(pt);
                    }
                    return ParseSuccessful(anotherparagraph);
                }
            }
            // abc \n def
            // That's not the end of a prargraph. Fallback to before the 1st \n .
            // Note here we have already consumed a \n .
            Fallback();
            return EMPTY_LINE_NODE;
        }

        /// <summary>
        /// LIST_ITEM
        /// </summary>
        private ListItem ParseListItem()
        {
            // We require all the list item starts at the beginning of a line,
            // i.e. "item" in {{T|* item}} will be treated as plain-text.
            // This will esp. prevent the leading whitespace of a template parameter be parsed into <pre>
            if (!BeginningOfLine()) return null;
            ParseStart();
            var prefix = ConsumeToken("[*#:;]+|-{4,}| ");
            if (prefix == null) return ParseFailed<ListItem>();
            var node = new ListItem { Prefix = prefix };
            ParseRun(RunParsingMode.Run, node, false); // optional
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
            for (var level = prefix.Length; level > 0; level--)
            {
                var barExpr = "={" + level + "}";
                // We use an early-stopping matching pattern
                // E.g. for ==abc=={{def}}=={{ghi}}
                // the first call to ParseRun will stop at =={{
                // we need to continue parsing, resulting in a list of segments
                // abc, {{def}}, {{ghi}}
                var headingTerminator = barExpr + "(?!=)";
                ParseStart(headingTerminator, false);   // <-- A
                var temp = ConsumeToken(barExpr);
                Debug.Assert(temp != null);
                var node = new Heading();
                var parsedSegments = new List<IInlineContainer>();
                while (true)
                {
                    ParseStart();                       // <-- B
                    var segment = new Run();
                    if (!ParseRun(RunParsingMode.Run, segment, true)
                        && LookAheadToken(headingTerminator) == null)
                    {
                        // No more content to parse, and ParseRun stopped by
                        // a terminator that is not a heading terminator
                        // Stop and analyze
                        Fallback();
                        break;
                    }
                    if (ConsumeToken(barExpr) == null)
                    {
                        // The segment has been parsed, but it's terminated not by "==="
                        // We treat the last segment as suffix
                        // Stop and analyze
                        node.Suffix = segment;
                        Accept();
                        break;
                    }
                    // Put the run segment into the list.
                    parsedSegments.Add(segment);
                }
                if (node.Suffix != null
                    && node.Suffix.Inlines.OfType<PlainText>().Any(pt => !string.IsNullOrWhiteSpace(pt.Content)))
                {
                    // There shouldn't be non-whitespace plaintext after the heading
                    goto FAIL_CLEANUP;
                }
                node.Level = level;
                // Concatenate segments, adding "===" where needed.
                for (int i = 0; i < parsedSegments.Count; i++)
                {
                    node.Inlines.AddFrom(parsedSegments[i].Inlines);
                    if (i < parsedSegments.Count - 1)
                    {
                        var si = (IWikitextLineInfo)parsedSegments[i + 1];
                        var bar = new PlainText(new string('=', level));
                        bar.SetLineInfo(si.StartLineNumber, si.StartLinePosition - level,
                            si.StartLineNumber, si.StartLinePosition);
                        node.Inlines.Add(bar);
                    }
                }
                if (node.Inlines.Count == 0)
                {
                    // There should be something as heading content
                    goto FAIL_CLEANUP;
                }
                // Move forward
                // -- B
                for (int i = 0; i < parsedSegments.Count; i++) Accept();
                return ParseSuccessful(node);   // <-- A
                FAIL_CLEANUP:
                // -- B
                for (int i = 0; i < parsedSegments.Count; i++) Fallback();
                Fallback();                     // <-- A
            }
            // Failed (E.g. ^=== Title )
            return null;
        }

        /// <summary>
        /// PARAGRAPH
        /// </summary>
        /// <remarks>The parsing operation will always succeed.</remarks>
        private LineNode ParseCompactParagraph(LineNode lastNode)
        {
            var mergeTo = lastNode as Paragraph;
            if (mergeTo != null && !mergeTo.Compact) mergeTo = null;
            // Create a new paragraph, or merge the new line to the last unclosed paragraph.
            ParseStart();
            if (mergeTo != null)
            {
                // This won't throw exception. See (A) in ParseLineEnd.
                var paraTail = (PlainText)mergeTo.Inlines.LastNode;
                paraTail.Content += "\n";
                IWikitextLineInfo paraTailSpan = paraTail;
                Debug.Assert(((IWikitextLineInfo)mergeTo).EndLinePosition == paraTailSpan.EndLinePosition);
                paraTail.ExtendLineInfo(paraTailSpan.EndLineNumber + 1, 0);
                mergeTo.ExtendLineInfo(paraTailSpan.EndLineNumber + 1, 0);
            }
            var node = mergeTo ?? new Paragraph();
            // Allows an empty paragraph/line.
            ParseRun(RunParsingMode.Run, node, false);
            if (mergeTo != null)
            {
                // Amend the line position
                lastNode.ExtendLineInfo(lineNumber, linePosition);
                return ParseSuccessful(EMPTY_LINE_NODE, false);
            }
            return ParseSuccessful(node);
        }

        /// <summary>
        /// RUN
        /// </summary>
        /// <returns><c>true</c> if one or more nodes has been parsed.</returns>
        private bool ParseRun(RunParsingMode mode, IInlineContainer container, bool setLineNumber)
        {
            ParseStart();
            var parsedAny = false;
            while (!NeedsTerminate())
            {
                // Read more
                InlineNode inline = null;
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
                if (inline is PlainText newtext)
                {
                    if (container.Inlines.LastNode is PlainText lastText)
                    {
                        lastText.Content += newtext.Content;
                        lastText.ExtendLineInfo(lineNumber, linePosition);
                        continue;
                    }
                }
                container.Inlines.Add(inline);
            }
            // Note that the content of RUN should not be empty.
            if (parsedAny)
            {
                ParseSuccessful((Node)container, setLineNumber);
                return true;
            }
            else
            {
                Fallback();
                return false;
            }
        }

        private InlineNode ParseInline()
        {
            return ParseTag()
                   ?? ParseWikiLink()
                   ?? ParseExternalLink()
                   ?? ParseFormatSwitch()
                   ?? (InlineNode)ParsePartialPlainText();
        }

        private InlineNode ParseExpandable()
        {
            return ParseComment() ?? ParseBraces();
        }

        private WikiLink ParseWikiLink()
        {
            // Note that wikilink cannot nest itself.
            ParseStart(@"\||\n|\[\[|\]\]", true);
            if (ConsumeToken(@"\[\[") == null) return ParseFailed<WikiLink>();
            var target = new Run();
            if (!ParseRun(RunParsingMode.ExpandableText, target, true))
            {
                if (options.AllowEmptyWikiLinkTarget)
                    target = null;
                else
                    return ParseFailed<WikiLink>();
            }
            var node = new WikiLink { Target = target };
            if (ConsumeToken(@"\|") != null)
            {
                var text = new Run();
                // Text accepts pipe
                CurrentContext.Terminator = Terminator.Get(@"\n|\[\[|\]\]");
                // For [[target|]], Text == Empty Run
                // For [[target]], Text == null
                if (ParseRun(RunParsingMode.ExpandableText, text, true))
                    node.Text = text;
            }
            if (ConsumeToken(@"\]\]") == null) return ParseFailed<WikiLink>();
            return ParseSuccessful(node);
        }

        private ExternalLink ParseExternalLink()
        {
            ParseStart(@"[\s\]\|]", true);
            var brackets = ConsumeToken(@"\[") != null;
            // Parse target
            Run target;
            if (brackets)
            {
                target = new Run();
                // Aggressive
                if (!ParseRun(RunParsingMode.ExpandableUrl, target, true))
                {
                    if (options.AllowEmptyExternalLinkTarget)
                        target = null;
                    else
                        return ParseFailed<ExternalLink>();
                }
            }
            else
            {
                // Conservative
                var url = ParseUrlText();
                if (url != null)
                {
                    target = new Run(url);
                    target.SetLineInfo(url);
                }
                else
                {
                    return ParseFailed<ExternalLink>();
                }
            }
            var node = new ExternalLink { Target = target, Brackets = brackets };
            if (brackets)
            {
                // Parse text
                if (ConsumeToken(@"[ \t]") != null)
                {
                    CurrentContext.Terminator = Terminator.Get(@"[\]\n]");
                    var text = new Run();
                    // For [http://target  ], Text == " "
                    // For [http://target ], Text == Empty Run
                    // For [http://target], Text == null
                    if (ParseRun(RunParsingMode.Run, text, true))
                        node.Text = text;
                }
                if (ConsumeToken(@"\]") == null) return ParseFailed(node);
            }
            return ParseSuccessful(node);
        }

        private FormatSwitch ParseFormatSwitch()
        {
            // For 4 or 5+ quotes, discard quotes on the left.
            ParseStart();
            var token = ConsumeToken("('{5}|'''|'')(?!')");
            if (token == null) return ParseFailed<FormatSwitch>();
            switch (token.Length)
            {
                case 2:
                    return ParseSuccessful(new FormatSwitch(false, true));
                case 3:
                    return ParseSuccessful(new FormatSwitch(true, false));
                case 5:
                    return ParseSuccessful(new FormatSwitch(true, true));
                default:
                    Debug.Assert(false);
                    return ParseFailed<FormatSwitch>();
            }
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
                    linePosition = 0;
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
            ParseStart();
            // Terminates now?
            if (NeedsTerminate()) return ParseFailed<PlainText>();
            // Or find the nearest terminator.
            // We'll at least consume 1 character.
            var terminatorPos = FindTerminator(1);
            // Remember, we'll at least consume 1 character.
            var susceptableEnd = SuspectablePlainTextEndMatcher.Match(fulltext, position + 1,
                terminatorPos - position - 1);
            var origPos = position;
            if (susceptableEnd.Success)
            {
                MovePositionTo(susceptableEnd.Index);
                return ParseSuccessful(new PlainText(fulltext.Substring(origPos, susceptableEnd.Index - origPos)));
            }
            else if (terminatorPos > 0)
            {
                MovePositionTo(terminatorPos);
                return ParseSuccessful(new PlainText(fulltext.Substring(origPos, terminatorPos - origPos)));
            }
            else
            {
                MovePositionTo(fulltext.Length);
                return ParseSuccessful(new PlainText(fulltext.Substring(origPos)));
            }
        }

        // From https://en.wikipedia.org/wiki/User:Cacycle/wikEd.js
        private const string UrlMatcher =
                @"(?i)\b(((https?:|ftp:|irc:|gopher:|)\/\/)|news:|mailto:)([^\x00-\x20\s""\[\]\x7f\|\{\}<>]|<[^>]*>)+?(?=([!""().,:;‘-•]*\s|[\x00-\x20\s""\[\]\x7f|{}]|$))"
            ;

        private PlainText ParseUrlText()
        {
            ParseStart();
            var url = ConsumeToken(UrlMatcher);
            if (url != null)
            {
                return ParseSuccessful(new PlainText(url));
            }
            return ParseFailed<PlainText>();
        }

        private enum RunParsingMode
        {
            /// <summary>
            /// Single line text.
            /// </summary>
            Run = 0,
            /// <summary>
            /// Single line text with EXPANDABLE.
            /// </summary>
            ExpandableText = 1,
            /// <summary>
            /// Single line URL with EXPANDABLE.
            /// </summary>
            ExpandableUrl = 2,
        }
    }
}
