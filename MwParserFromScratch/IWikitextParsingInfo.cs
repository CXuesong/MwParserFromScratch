using System;
using System.Collections.Generic;
using System.Text;

namespace MwParserFromScratch;

/// <summary>
/// Provides extra information of parsing process.
/// </summary>
public interface IWikitextParsingInfo
{

    /// <summary>
    /// Wether the closing mark of a template (}}) or an HTML tag (&lt;/ xxx&gt;) is implicitly
    /// inferred by the wikitext parser.
    /// </summary>
    bool InferredClosingMark { get; }

}
