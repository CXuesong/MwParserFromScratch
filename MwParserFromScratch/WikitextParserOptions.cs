using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MwParserFromScratch.Nodes;

namespace MwParserFromScratch
{
    /// <summary>
    /// Used to specify the settings for <see cref="WikitextParser"/>.
    /// </summary>
    public class WikitextParserOptions
    {
        #region Presets

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

        internal static readonly HashSet<string> DefaultCaseInsensitiveMagicTemplatesSet =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ARTICLEPATH",
                "PAGEID",
                "SERVER",
                "SERVERNAME",
                "SCRIPTPATH",
                "STYLEPATH",
                // $noHashFunctions
                "NS",
                "NSE",
                "URLENCODE",
                "LCFIRST",
                "UCFIRST",
                "LC",
                "UC",
                "LOCALURL",
                "LOCALURLE",
                "FULLURL",
                "FULLURLE",
                "CANONICALURL",
                "CANONICALURLE",
                "FORMATNUM",
                "GRAMMAR",
                "GENDER",
                "PLURAL",
                "BIDI",
                "PADLEFT",
                "PADRIGHT",
                "ANCHORENCODE",
                "FILEPATH",
                "PAGEID",
                // Transclusion modifiers
                "INT",
                "MSG",
                "RAW",
                "MSGNW",
                "SUBST"
            };

        internal static readonly HashSet<string> DefaultCaseSensitiveMagicTemplatesSet =
            new HashSet<string>
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
                "SITENAME",
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
                "NUMBEROFPAGES",
                "NUMBEROFUSERS",
                "NUMBEROFACTIVEUSERS",
                "NUMBEROFARTICLES",
                "NUMBEROFFILES",
                "NUMBEROFADMINS",
                "NUMBERINGROUP",
                "NUMBEROFEDITS",
                "LANGUAGE",
                "DEFAULTSORT",
                "PAGESINCATEGORY",
                "PAGESIZE",
                "PROTECTIONLEVEL",
                "PROTECTIONEXPIRY",
                "NAMESPACEE",
                "NAMESPACENUMBER",
                "TALKSPACE",
                "TALKSPACEE",
                "SUBJECTSPACE",
                "SUBJECTSPACEE",
                "PAGENAME",
                "PAGENAMEE",
                "FULLPAGENAME",
                "FULLPAGENAMEE",
                "ROOTPAGENAME",
                "ROOTPAGENAMEE",
                "BASEPAGENAME",
                "BASEPAGENAMEE",
                "SUBPAGENAME",
                "SUBPAGENAMEE",
                "TALKPAGENAME",
                "TALKPAGENAMEE",
                "SUBJECTPAGENAME",
                "SUBJECTPAGENAMEE",
                "REVISIONID",
                "REVISIONDAY",
                "REVISIONDAY2",
                "REVISIONMONTH",
                "REVISIONMONTH1",
                "REVISIONYEAR",
                "REVISIONTIMESTAMP",
                "REVISIONUSER",
                "CASCADINGSOURCES",
                // Appendix & Aliases
                "DISPLAYTITLE",
                "DEFAULTSORTKEY",
                "DEFAULTCATEGORYSORT",
                "PAGESINNS"
            };
        #endregion

        public static readonly IReadOnlyList<MagicTemplateNameInfo> DefaultMagicTemplateNames
            = new ReadOnlyCollection<MagicTemplateNameInfo>(DefaultCaseSensitiveMagicTemplatesSet
                .Select(n => new MagicTemplateNameInfo(n, false))
                .Concat(DefaultCaseSensitiveMagicTemplatesSet.Select(n => new MagicTemplateNameInfo(n, true)))
                .ToArray());

        private static readonly HashSet<string> DefaultParserTagsSet = new HashSet<string>(DefaultParserTags, StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> DefaultSelfClosingOnlyTagsSet = new HashSet<string>(DefaultSelfClosingOnlyTags, StringComparer.OrdinalIgnoreCase);

        internal static WikitextParserOptions DefaultOptionsCopy = new WikitextParserOptions().DefensiveCopy();

        private IEnumerable<string> _ParserTags;
        private IEnumerable<string> _SelfClosingOnlyTags;
        private IEnumerable<MagicTemplateNameInfo> _MagicTemplateNames;
        private bool _AllowEmptyTemplateName;
        private bool _AllowEmptyWikiLinkTarget;
        private bool _AllowEmptyExternalLinkTarget;
        private bool _AllowClosingMarkInference;
        private bool _WithLineInfo;
        private volatile WikitextParserOptions _DefensiveCopy;

        /// <summary>
        /// Names of the parser tags. E.g. gallery .
        /// Tag names are case-insensitive.
        /// </summary>
        /// <value>A list of strings, which are valid tag names. OR <c>null</c> to use the default settings.</value>
        public IEnumerable<string> ParserTags
        {
            get { return _ParserTags; }
            set
            {
                _ParserTags = value;
                _DefensiveCopy = null;
            }
        }

        /// <summary>
        /// Names of tags that can only be used in a self-closing way.
        /// </summary>
        /// <remarks>The default value is "br", "wbr", "hr".</remarks>
        public IEnumerable<string> SelfClosingOnlyTags
        {
            get { return _SelfClosingOnlyTags; }
            set
            {
                _SelfClosingOnlyTags = value;
                _DefensiveCopy = null;
            }
        }

        /// <summary>
        /// Names of the variables and parser functions in Wikitext. E.g. PAGENAME.
        /// </summary>
        /// <value>A list of strings, which are valid variable names. OR <c>null</c> to use the default settings.</value>
        /// <remarks>See https://www.mediawiki.org/wiki/Help:Magic_words#Variables .</remarks>
        public IEnumerable<MagicTemplateNameInfo> MagicTemplateNames
        {
            get { return _MagicTemplateNames; }
            set
            {
                _MagicTemplateNames = value;
                _DefensiveCopy = null;
            }
        }

        /// <summary>
        /// When parsing for template transclusions, allows empty template names.
        /// </summary>
        /// <remarks>For empty template names, the <see cref="Template.Name"/> will be <c>null</c>.</remarks>
        public bool AllowEmptyTemplateName
        {
            get { return _AllowEmptyTemplateName; }
            set { _AllowEmptyTemplateName = value; _DefensiveCopy = null; }
        }

        /// <summary>
        /// When parsing for wikilinks, allows empty link targets.
        /// </summary>
        /// <remarks>For empty wikilink targets, the <see cref="WikiLink.Target"/> will be <c>null</c>.</remarks>
        public bool AllowEmptyWikiLinkTarget
        {
            get { return _AllowEmptyWikiLinkTarget; }
            set { _AllowEmptyWikiLinkTarget = value; _DefensiveCopy = null; }
        }

        /// <summary>
        /// When parsing for external links, allows empty link targets.
        /// </summary>
        /// <remarks>
        /// <para>For empty wikilink targets, the <see cref="ExternalLink.Target"/> will be <c>null</c>.</para>
        /// <para>It's recommended this property set to <c>false</c>.</para>
        /// </remarks>
        public bool AllowEmptyExternalLinkTarget
        {
            get { return _AllowEmptyExternalLinkTarget; }
            set { _AllowEmptyExternalLinkTarget = value; _DefensiveCopy = null; }
        }

        ///// <summary>
        ///// When parsing for wikilinks, templates, and HTML tags, allows inference of missing close marks.
        ///// </summary>
        //public bool AllowClosingMarkInference
        //{
        //    get { return _AllowClosingMarkInference; }
        //    set { _AllowClosingMarkInference = value; _DefensiveCopy = null; }
        //}

        public bool WithLineInfo
        {
            get { return _WithLineInfo; }
            set { _WithLineInfo = value; _DefensiveCopy = null; }
        }

        internal ISet<string> ParserTagsSet => (ISet<string>) ParserTags;

        internal ISet<string> SelfClosingOnlyTagsSet => (ISet<string>) SelfClosingOnlyTags;

        internal ISet<string> CaseSensitiveMagicTemplateNamesSet { get; private set; }

        internal ISet<string> CaseInsensitiveMagicTemplateNamesSet { get; private set; }

        internal WikitextParserOptions DefensiveCopy()
        {
            // This method should be thread-safe when there are concurrent DefensiveCopy calls.
            if (_DefensiveCopy != null) return _DefensiveCopy;
            var inst = (WikitextParserOptions) MemberwiseClone();
            inst._DefensiveCopy = inst;
            inst._ParserTags = ParserTags == null || ReferenceEquals(ParserTags, DefaultParserTags)
                ? DefaultParserTagsSet
                : new HashSet<string>(ParserTags, StringComparer.OrdinalIgnoreCase);
            inst._SelfClosingOnlyTags = SelfClosingOnlyTags == null ||
                                        ReferenceEquals(SelfClosingOnlyTags, DefaultSelfClosingOnlyTags)
                ? DefaultSelfClosingOnlyTagsSet
                : new HashSet<string>(SelfClosingOnlyTags, StringComparer.OrdinalIgnoreCase);
            if (inst.MagicTemplateNames == null || ReferenceEquals(MagicTemplateNames, DefaultMagicTemplateNames))
            {
                inst.CaseSensitiveMagicTemplateNamesSet = DefaultCaseSensitiveMagicTemplatesSet;
                inst.CaseInsensitiveMagicTemplateNamesSet = DefaultCaseInsensitiveMagicTemplatesSet;
            }
            else
            {
                inst.CaseSensitiveMagicTemplateNamesSet = new HashSet<string>();
                inst.CaseInsensitiveMagicTemplateNamesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var tn in MagicTemplateNames)
                {
                    if (tn.IsCaseSensitive) inst.CaseSensitiveMagicTemplateNamesSet.Add(tn.Name);
                    else inst.CaseInsensitiveMagicTemplateNamesSet.Add(tn.Name);
                }
            }
            inst._MagicTemplateNames = null;
            _DefensiveCopy = inst;
            return inst;
        }

    }

    /// <summary>
    /// An entry contains the name of a variable or a parser function,
    /// and whether the name is case-sensitive.
    /// </summary>
    public struct MagicTemplateNameInfo
    {
        public MagicTemplateNameInfo(string name, bool isCaseSensitive)
        {
            Name = name;
            IsCaseSensitive = isCaseSensitive;
        }

        /// <summary>
        /// Name of the variable or parser function.
        /// For parser functions, the value may start with #.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Whether the name is case-sensitive.
        /// </summary>
        public bool IsCaseSensitive { get; }
    }
}
