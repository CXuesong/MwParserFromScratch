using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MwParserFromScratch.Nodes;

namespace MwParserFromScratch
{
    /// <summary>
    /// Provides the starting position and the length (a.k.a. the span) of a <see cref="Node"/> with respect to the parsed wikitext string.
    /// </summary>
    /// <remarks>The span indicates the substring [Start, Start + Length) of the parsed string.</remarks>
    public interface IWikitextSpanInfo
    {
        /// <summary>
        /// Gets the position of the span's beginning.
        /// </summary>
        /// <remarks>The zero-based starting character position of this node, with respect to the parsed wikitext, or 0 if there's no span information available (for example, <see cref="HasSpanInfo"/> returns false).</remarks>
        int Start { get; }

        /// <summary>
        /// Gets the span's length.
        /// </summary>
        /// <remarks>The number of characters of this node, with respect to the parsed wikitext, or 0 if there's no span information available (for example, <see cref="HasSpanInfo"/> returns false).</remarks>
        int Length { get; }

        /// <summary>
        /// Gets a value indicating whether the instance can return span information.
        /// </summary>
        bool HasSpanInfo { get; }
    }
}
