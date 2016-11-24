using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MwParserFromScratch
{
    /// <summary>
    /// Exposes methods to trace the behavior of <see cref="WikitextParser"/>.
    /// </summary>
    public interface IWikitextParserLogger
    {
        void NotifyStartParsing(string text);

        /// <summary>
        /// Called when a fallback in parsing has happened during parsing process.
        /// </summary>
        /// <param name="offset">Current character index, before falling back.</param>
        /// <param name="contextStackSize">The size of context stack. This may reflect the depth of parsing.</param>
        void NotifyFallback(int offset, int contextStackSize);

        void NotifyStopParsing();
    }
}
