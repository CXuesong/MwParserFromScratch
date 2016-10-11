using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MwParserFromScratch
{
    /// <summary>
    /// Provides an interface to enable a class to return line and position information.
    /// </summary>
    public interface IWikitextLineInfo
    {
        /// <summary>
        /// Gets the current line number. 
        /// </summary>
        /// <remarks>The current line number or 0 if no line information is available (for example, HasLineInfo returns false).</remarks>
        int LineNumber { get; }
        /// <summary>
        /// Gets the current line position. 
        /// </summary>
        /// <remarks>The current line position or 0 if no line information is available (for example, HasLineInfo returns false).</remarks>
        int LinePosition { get; }

        /// <summary>
        /// Gets a value indicating whether the class can return line information.
        /// </summary>
        bool HasLineInfo();
    }
}
