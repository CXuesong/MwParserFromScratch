using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MwParserFromScratch.Nodes;

namespace MwParserFromScratch;

/// <summary>
/// Provides the starting position and ending position of a <see cref="Node"/> with respect to the parsed wikitext string.
/// </summary>
public interface IWikitextLineInfo
{
    /// <summary>
    /// Gets the 0-based starting line number.
    /// </summary>
    /// <remarks>The current line number or 0 if no line information is available (for example, <see cref="HasLineInfo"/> returns <c>false</c>).</remarks>
    int StartLineNumber { get; }

    /// <summary>
    /// Gets the 0-based starting character position in the line.
    /// </summary>
    /// <remarks>The current line number or 0 if no line information is available (for example, <see cref="HasLineInfo"/> returns <c>false</c>).</remarks>
    int StartLinePosition { get; }

    /// <summary>
    /// Gets the 0-based ending line number.
    /// </summary>
    /// <remarks>The current line number or 0 if no line information is available (for example, <see cref="HasLineInfo"/> returns <c>false</c>).</remarks>
    int EndLineNumber { get; }

    /// <summary>
    /// Gets the 0-based exclusive starting character position in the line.
    /// </summary>
    /// <remarks>This is the starting index of the first character outside the node.
    /// The current line number or 0 if no line information is available (for example, <see cref="HasLineInfo"/> returns <c>false</c>).</remarks>
    int EndLinePosition { get; }

    /// <summary>
    /// Gets a value indicating whether the class can return line information.
    /// </summary>
    bool HasLineInfo { get; }
}
