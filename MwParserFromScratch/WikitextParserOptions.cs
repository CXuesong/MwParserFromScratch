using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MwParserFromScratch
{
    /// <summary>
    /// Used to specify the settings for <see cref="WikitextParser"/>.
    /// </summary>
    public class WikitextParserOptions
    {
        public static readonly IReadOnlyList<string> DefaultParserTags =
            new ReadOnlyCollection<string>(new[]
            {
                // Built-ins
                "gallery", "includeonly", "noinclude", "nowiki", "onlyinclude", "pre",
                // Extensions
                "categorytree", "charinsert", "dynamicpagelist", "graph", "hiero", "imagemap", "indicator", "inputbox",
                "languages", "math", "poem", "ref", "references", "score", "section", "syntaxhighlight", "source",
                "templatedata", "timeline",
            });

        public static readonly IReadOnlyList<string> DefaultParserFunctions =
            new ReadOnlyCollection<string>(new[] {"PAGENAME"});

        /// <summary>
        /// Names of the parser tags. E.g. gallery .
        /// Tag names are case-insensitive.
        /// </summary>
        /// <value>A list of strings, which are valid tag names. OR <c>null</c> to use the default settings.</value>
        public IList<string> ParserTags { get; set; }

        /// <summary>
        /// Names of the parser functions. E.g. PAGENAME .
        /// Parser function names are case-insensitive.
        /// </summary>
        /// <value>A list of strings, which are valid parser function names. OR <c>null</c> to use the default settings.</value>
        /// <remarks>In current implementation, all parser functions will be treated as templates.</remarks>
        public IList<string> ParserFunctions { get; set; }
    }
}
