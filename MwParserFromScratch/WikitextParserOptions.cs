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

        public static readonly IReadOnlyList<string> DefaultSelfClosingOnlyTags =
            new ReadOnlyCollection<string>(new[]
            {
                "br", "wbr", "hr"
            });

        public static readonly IReadOnlyList<string> DefaultVariableNames =
            new ReadOnlyCollection<string>(new[]
            {
                // Real variables
                "!",
                "CURRENTMONTH",
                "CURRENTMONTH1",
                "CURRENTMONTHNAME",
                "CURRENTMONTHNAMEGEN",
                "CURRENTMONTHABBREV",
                "CURRENTDAY",
                "CURRENTDAY2",
                "CURRENTDAYNAME",
                "CURRENTYEAR",
                "CURRENTTIME",
                "CURRENTHOUR",
                "LOCALMONTH",
                "LOCALMONTH1",
                "LOCALMONTHNAME",
                "LOCALMONTHNAMEGEN",
                "LOCALMONTHABBREV",
                "LOCALDAY",
                "LOCALDAY2",
                "LOCALDAYNAME",
                "LOCALYEAR",
                "LOCALTIME",
                "LOCALHOUR",
                "NUMBEROFARTICLES",
                "NUMBEROFFILES",
                "NUMBEROFEDITS",
                "ARTICLEPATH",
                "PAGEID",
                "SITENAME",
                "SERVER",
                "SERVERNAME",
                "SCRIPTPATH",
                "STYLEPATH",
                "PAGENAME",
                "PAGENAMEE",
                "FULLPAGENAME",
                "FULLPAGENAMEE",
                "NAMESPACE",
                "NAMESPACEE",
                "NAMESPACENUMBER",
                "CURRENTWEEK",
                "CURRENTDOW",
                "LOCALWEEK",
                "LOCALDOW",
                "REVISIONID",
                "REVISIONDAY",
                "REVISIONDAY2",
                "REVISIONMONTH",
                "REVISIONMONTH1",
                "REVISIONYEAR",
                "REVISIONTIMESTAMP",
                "REVISIONUSER",
                "REVISIONSIZE",
                "SUBPAGENAME",
                "SUBPAGENAMEE",
                "TALKSPACE",
                "TALKSPACEE",
                "SUBJECTSPACE",
                "SUBJECTSPACEE",
                "TALKPAGENAME",
                "TALKPAGENAMEE",
                "SUBJECTPAGENAME",
                "SUBJECTPAGENAMEE",
                "NUMBEROFUSERS",
                "NUMBEROFACTIVEUSERS",
                "NUMBEROFPAGES",
                "CURRENTVERSION",
                "ROOTPAGENAME",
                "ROOTPAGENAMEE",
                "BASEPAGENAME",
                "BASEPAGENAMEE",
                "CURRENTTIMESTAMP",
                "LOCALTIMESTAMP",
                "DIRECTIONMARK",
                "CONTENTLANGUAGE",
                "NUMBEROFADMINS",
                "CASCADINGSOURCES",
                // $noHashFunctions
                "NS", "NSE", "URLENCODE", "LCFIRST", "UCFIRST", "LC", "UC",
                "LOCALURL", "LOCALURLE", "FULLURL", "FULLURLE", "CANONICALURL",
                "CANONICALURLE", "FORMATNUM", "GRAMMAR", "GENDER", "PLURAL", "BIDI",
                "NUMBEROFPAGES", "NUMBEROFUSERS", "NUMBEROFACTIVEUSERS",
                "NUMBEROFARTICLES", "NUMBEROFFILES", "NUMBEROFADMINS",
                "NUMBERINGROUP", "NUMBEROFEDITS", "LANGUAGE",
                "PADLEFT", "PADRIGHT", "ANCHORENCODE", "DEFAULTSORT", "FILEPATH",
                "PAGESINCATEGORY", "PAGESIZE", "PROTECTIONLEVEL", "PROTECTIONEXPIRY",
                "NAMESPACEE", "NAMESPACENUMBER", "TALKSPACE", "TALKSPACEE",
                "SUBJECTSPACE", "SUBJECTSPACEE", "PAGENAME", "PAGENAMEE",
                "FULLPAGENAME", "FULLPAGENAMEE", "ROOTPAGENAME", "ROOTPAGENAMEE",
                "BASEPAGENAME", "BASEPAGENAMEE", "SUBPAGENAME", "SUBPAGENAMEE",
                "TALKPAGENAME", "TALKPAGENAMEE", "SUBJECTPAGENAME",
                "SUBJECTPAGENAMEE", "PAGEID", "REVISIONID", "REVISIONDAY",
                "REVISIONDAY2", "REVISIONMONTH", "REVISIONMONTH1", "REVISIONYEAR",
                "REVISIONTIMESTAMP", "REVISIONUSER", "CASCADINGSOURCES",
            });

        internal static readonly HashSet<string> DefaultParserTagsSet = new HashSet<string>(DefaultParserTags, StringComparer.OrdinalIgnoreCase);
        internal static readonly HashSet<string> DefaultSelfClosingOnlyTagsSet = new HashSet<string>(DefaultSelfClosingOnlyTags, StringComparer.OrdinalIgnoreCase);
        internal static readonly HashSet<string> DefaultVariableNamesSet = new HashSet<string>(DefaultVariableNames);

        /// <summary>
        /// Names of the parser tags. E.g. gallery .
        /// Tag names are case-insensitive.
        /// </summary>
        /// <value>A list of strings, which are valid tag names. OR <c>null</c> to use the default settings.</value>
        public IEnumerable<string> ParserTags { get; set; }

        ///// <summary>
        ///// Names of the parser functions. E.g. #if .
        ///// Parser function names are case-insensitive.
        ///// </summary>
        ///// <value>A list of strings, which are valid parser function names. OR <c>null</c> to use the default settings.</value>
        //public IEnumerable<string> ParserFunctions { get; set; }

        /// <summary>
        /// Names of tags that can only be used in a self-closing way.
        /// </summary>
        /// <remarks>The default value is "br", "wbr", "hr".</remarks>
        public IEnumerable<string> SelfClosingOnlyTags { get; set; }

        /// <summary>
        /// Names of the variables in Wikitext. E.g. PAGENAME.
        /// </summary>
        /// <value>A list of strings, which are valid variable names. OR <c>null</c> to use the default settings.</value>
        /// <remarks>See https://www.mediawiki.org/wiki/Help:Magic_words#Variables .</remarks>
        public IEnumerable<string> VariableNames { get; set; }
    }
}
