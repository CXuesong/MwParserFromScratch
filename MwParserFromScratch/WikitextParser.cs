using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using MwParserFromScratch.Nodes;

namespace MwParserFromScratch
{
    /// <summary>
    /// A parser that parses Wikitext into AST.
    /// </summary>
    /// <remarks>This class is thread-safe.</remarks>
    public class WikitextParser
    {
        private ParserCore cachedCore;

        public WikitextParser()
        {
        }

        /// <summary>
        /// The options, or <c>null</c> to use default options.
        /// </summary>
        public WikitextParserOptions Options { get; set; }

        /// <summary>
        /// A logger used to trace the process of parsing.
        /// </summary>
        public IWikitextParserLogger Logger { get; set; }

        /// <summary>
        /// Parses the specified Wikitext.
        /// </summary>
        /// <param name="wikitext">The wikitext to be parsed.</param>
        /// <returns>A <see cref="Wikitext"/> node containing the AST of the given Wikitext.</returns>
        /// <exception cref="InvalidParserStateException">The parser state is invalid during the parsing process. There might be a b&#117;g with the parser.</exception>
        /// <exception cref="OperationCanceledException">The parsing process has been cancelled.</exception>
        public Wikitext Parse(string wikitext)
        {
            return Parse(wikitext, CancellationToken.None);
        }

        /// <summary>
        /// Parses the specified Wikitext.
        /// </summary>
        /// <param name="wikitext">The wikitext to be parsed.</param>
        /// <param name="cancellationToken">The token used to cancel the parsing operation.</param>
        /// <returns>A <see cref="Wikitext"/> node containing the AST of the given Wikitext.</returns>
        /// <exception cref="InvalidParserStateException">The parser state is invalid during the parsing process. There might be a b&#117;g with the parser.</exception>
        /// <exception cref="OperationCanceledException">The parsing process has been cancelled.</exception>
        public Wikitext Parse(string wikitext, CancellationToken cancellationToken)
        {
            if (wikitext == null) throw new ArgumentNullException(nameof(wikitext));
            cancellationToken.ThrowIfCancellationRequested();
            var core = Interlocked.Exchange(ref cachedCore, null);
            if (core == null) core = new ParserCore();
            try
            {
                return core.Parse(Options, Logger, wikitext, cancellationToken);
            }
            finally
            {
                Interlocked.CompareExchange(ref cachedCore, core, null);
            }
        }
    }
}
