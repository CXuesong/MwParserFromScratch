using System.Diagnostics;
using System.Text.RegularExpressions;
using MwParserFromScratch.Nodes;

namespace MwParserFromScratch;

internal partial class ParserCore
{

    private IWikitextParserLogger logger;
    private WikitextParserOptions options;

    private string fulltext;
    private int position; // Starting index of the string to be consumed.
    private int lineNumber, linePosition;
    private Stack<ParsingContext> contextStack;
    private CancellationToken cancellationToken;
    private static readonly Dictionary<string, Regex> tokenMatcherCache = new();

    public ParserCore()
    {
    }

    public Wikitext Parse(WikitextParserOptions options, IWikitextParserLogger logger, string wikitext,
        CancellationToken cancellationToken)
    {
        if (wikitext == null) throw new ArgumentNullException(nameof(wikitext));
        cancellationToken.ThrowIfCancellationRequested();
        // Initialize
        this.options = options?.DefensiveCopy() ?? WikitextParserOptions.DefaultOptionsCopy;
        this.logger = logger;
        fulltext = wikitext;
        lineNumber = linePosition = 0;
        position = 0;
        contextStack = new Stack<ParsingContext>();
        this.cancellationToken = cancellationToken;
        try
        {
            // Then parse
            logger?.NotifyParsingStarted(wikitext);
            var root = ParseWikitext();
            // State check
            if (position < fulltext.Length)
                throw new InvalidParserStateException(
                    "Detected unparsed wikitext after parsing. There might be a bug with parser.");
            if (contextStack.Count > 0)
                throw new InvalidParserStateException(
                    "Detected remaining ParsingContext on context stack. There might be a bug with parser.");
            logger?.NotifyParsingFinished();
            return root;
        }
        finally
        {
            // Cleanup
            fulltext = null;
            contextStack = null;
            options = null;
            logger = null;
        }
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
    private int FindTerminator(int skippedCharacters)
    {
        if (position + skippedCharacters >= fulltext.Length)
            return fulltext.Length;
        int index = fulltext.Length;

        // Find the left-most terminator over all the context frames
        foreach (var context in contextStack)
        {
            var newIndex = context.FindTerminator(fulltext, position + skippedCharacters, logger);
            if (newIndex >= 0 && newIndex < index)
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

    /// <summary>
    /// Accept, and optionally set the line number of the node.
    /// </summary>
    private T ParseSuccessful<T>(T value, bool setLineNumber = true) where T : Node
    {
        Debug.Assert(value != null);
        // At least we've consumed something. Or empty WIKITEXT.
        Debug.Assert(position >= CurrentContext.StartingPosition);
        if (setLineNumber)
        {
            value.SetLineInfo(CurrentContext.StartingLineNumber,
                CurrentContext.StartingLinePosition,
                lineNumber, linePosition);
        }
        Accept();
        return value;
    }

    /// <summary>
    /// Accept the characters consumed in the current context.
    /// You won't be able to backtrack to the position where last <see cref="ParseStart()"/> call takes place.
    /// </summary>
    private void Accept()
    {
        contextStack.Pop();
    }

    private T ParseFailed<T>(T node = default(T)) where T : Node
    {
        Fallback();
        return default(T);
    }

    private void Fallback()
    {
        logger?.NotifyFallback(position, contextStack.Count);
        var context = contextStack.Pop();
        // Fallback
        position = context.StartingPosition;
        lineNumber = context.StartingLineNumber;
        linePosition = context.StartingLinePosition;
    }

    private bool BeginningOfLine()
    {
        return linePosition == 0;
    }

    /// <summary>
    /// Match the next token. This operation will not consume the tokens.
    /// </summary>
    /// <param name="tokenMatcher">A regular expression string used to match the next token.</param>
    /// <returns>The string of token that has been successfully matched. OR <c>null</c> is such attempt failed.</returns>
    private string? LookAheadToken(string tokenMatcher)
    {
        Debug.Assert(tokenMatcher[0] != '^');
        Regex? re;
        lock (tokenMatcherCache)
        {
            re = tokenMatcherCache.GetValueOrDefault(tokenMatcher);
            if (re == null)
            {
                // The occurence should be starting from current position.
                re = new Regex(@"\G(" + tokenMatcher + ")");
                tokenMatcherCache.Add(tokenMatcher, re);
            }
        }
        var m = re.Match(fulltext, position);
        if (!m.Success) return null;
        Debug.Assert(position == m.Index);
        // We want to move forward.
        Debug.Assert(m.Length > 0, "A successful matching should consume at least 1 character.");
        return m.Value;
    }

    /// <summary>
    /// Attempts to consume the next token.
    /// </summary>
    /// <param name="tokenMatcher">A regular expression string used to match the next token.</param>
    /// <returns>The string of token that has been successfully consumed. OR <c>null</c> is such attempt failed.</returns>
    private string? ConsumeToken(string tokenMatcher)
    {
        var t = LookAheadToken(tokenMatcher);
        if (t == null) return null;
        // The position indicates the beginning of the next token.
        MovePositionTo(position + t.Length);
        return t;
    }

    [DebuggerDisplay("StartingPosition={StartingPosition}({StartingLineNumber},{StartingLinePosition}), Terminator={Terminator}, OverridesTerminator={OverridesTerminator}")]
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

        public int FindTerminator(string str, int startIndex, IWikitextParserLogger logger)
        {
            Debug.Assert(str != null);
            if (Terminator == null) return -1;
            return Terminator.Search(str, startIndex, logger);
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
        public int Search(string str, int startIndex, IWikitextParserLogger logger)
        {
            logger?.NotifyRegexMatchingStarted(startIndex, searcher);
            var match = searcher.Match(str, startIndex);
            logger?.NotifyRegexMatchingFinished(startIndex, searcher);
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
            lock (cacheDict)
            {
                var t = cacheDict.GetValueOrDefault(regularExpr);
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