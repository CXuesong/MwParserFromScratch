using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MwParserFromScratch.Nodes
{
    public abstract class InlineNode : Node
    {

    }

    public class PlainText : InlineNode
    {
        public PlainText() : this(null)
        {
        }

        public PlainText(string content)
        {
            Content = content;
        }

        public string Content { get; set; }

        /// <summary>
        /// Infrastructure. Enumerates the children of this node.
        /// </summary>
        /// <returns>Always an empty sequence of nodes.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override IEnumerable<Node> EnumChildren()
            => Enumerable.Empty<Node>();

        protected override Node CloneCore()
        {
            return new PlainText { Content = Content };
        }

        public override string ToString()
        {
            return Content;
        }

        /// <inheritdoc />
        public override string ToPlainText(NodePlainTextOptions options)
        {
            // Unescape HTML entities.
            return WebUtility.HtmlDecode(Content);
        }
    }

    public class WikiLink : InlineNode
    {
        private Run _Target;
        private Run _Text;

        public Run Target
        {
            get { return _Target; }
            set { Attach(ref _Target, value); }
        }

        public Run Text
        {
            get { return _Text; }
            set { Attach(ref _Text, value); }
        }

        /// <summary>
        /// Enumerates the children of this node.
        /// </summary>
        public override IEnumerable<Node> EnumChildren()
        {
            if (_Target != null) yield return _Target;
            if (_Text != null) yield return _Text;
        }

        protected override Node CloneCore()
        {
            return new WikiLink { Target = Target, Text = Text };
        }

        public override string ToString() => Text == null ? $"[[{Target}]]" : $"[[{Target}|{Text}]]";

        private static readonly Regex PipeTrickTitleMatcher = new Regex(@".+?(?=\w*\()");

        /// <inheritdoc />
        public override string ToPlainText(NodePlainTextOptions options)
        {
            if (Text != null)
            {
                if (Text.Inlines.Count == 0)
                {
                    // Pipe trick. E.g.
                    // [[abc (disambiguation)|]]
                    var original = Target.ToPlainText(options);
                    var match = PipeTrickTitleMatcher.Match(original);
                    if (match.Success) return match.Value;
                    return original;
                }
                else
                {
                    return Text.ToPlainText(options);
                }
            }
            return Target.ToPlainText(options);
        }
    }

    public class ExternalLink : InlineNode
    {
        private Run _Target;
        private Run _Text;

        public Run Target
        {
            get { return _Target; }
            set { Attach(ref _Target, value); }
        }

        /// <summary>
        /// Display text of the link.
        /// </summary>
        /// <value>
        /// Display text of the link, or <c>null</c>, if the link url is just surrounded by
        /// a pair of square brackets. (e.g. <c>[http://abc.def]</c>).
        /// </value>
        public Run Text
        {
            get { return _Text; }
            set { Attach(ref _Text, value); }
        }

        /// <summary>
        /// Whether the link is contained in square brackets.
        /// </summary>
        public bool Brackets { get; set; }

        /// <summary>
        /// Enumerates the children of this node.
        /// </summary>
        public override IEnumerable<Node> EnumChildren()
        {
            if (_Target != null) yield return _Target;
            if (_Text != null) yield return _Text;
        }

        protected override Node CloneCore()
        {
            return new ExternalLink { Target = Target, Text = Text };
        }

        public override string ToString()
        {
            var s = Target?.ToString();
            if (Text != null) s += " " + Text;
            if (Brackets) s = "[" + s + "]";
            return s;
        }

        /// <inheritdoc />
        public override string ToPlainText(NodePlainTextOptions options)
        {
            if (!Brackets) return Target.ToPlainText(options);
            var text = Text?.ToPlainText(options);
            // We should have shown something like [1]
            if (string.IsNullOrWhiteSpace(text)) return "[#]";
            return text;
        }
    }

    /// <summary>
    /// Represents wikitext with bold / italics.
    /// </summary>
    public class FormatSwitch : InlineNode
    {
        public FormatSwitch() : this(false, false)
        {
        }

        public FormatSwitch(bool switchBold, bool switchItalics)
        {
            SwitchBold = switchBold;
            SwitchItalics = switchItalics;
        }

        /// <summary>
        /// Whether to switch font-bold of the incoming content.
        /// </summary>
        public bool SwitchBold { get; set; }

        /// <summary>
        /// Whether to switch font-italics of the incoming content.
        /// </summary>
        public bool SwitchItalics { get; set; }

        /// <summary>
        /// Infrastructure. Enumerates the children of this node.
        /// </summary>
        /// <returns>Always an empty sequence of nodes.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override IEnumerable<Node> EnumChildren()
            => Enumerable.Empty<Node>();

        protected override Node CloneCore()
        {
            var n = new FormatSwitch { SwitchBold = SwitchBold, SwitchItalics = SwitchItalics };
            return n;
        }

        public override string ToString()
        {
            if (SwitchBold && SwitchItalics)
                return "'''''";
            if (SwitchBold)
                return "'''";
            if (SwitchItalics)
                return "''";
            return "";
        }

        /// <inheritdoc />
        public override string ToPlainText(NodePlainTextOptions options)
        {
            return null;
        }
    }

    /// <summary>
    /// Represents all the template-like formations, including Variables and Parser Functions.
    /// </summary>
    public class Template : InlineNode
    {
        private Run _Name;

        public Template() : this(null)
        {
        }

        public Template(Run name)
        {
            Name = name;
            Arguments = new TemplateArgumentCollection(this);
        }

        /// <summary>
        /// Title of the template page to transclude.
        /// </summary>
        public Run Name
        {
            get { return _Name; }
            set { Attach(ref _Name, value); }
        }

        /// <summary>
        /// Whether this node is a Variable or Parser Function.
        /// This will affect how the first argument is rendered in wikitext.
        /// </summary>
        public bool IsMagicWord { get; set; }

        /// <summary>
        /// Template arguments.
        /// </summary>
        public TemplateArgumentCollection Arguments { get; }

        /// <summary>
        /// Enumerates the children of this node.
        /// </summary>
        public override IEnumerable<Node> EnumChildren()
        {
            if (_Name != null) yield return _Name;
            foreach (var arg in Arguments) yield return arg;
        }

        protected override Node CloneCore()
        {
            var n = new Template { Name = Name };
            n.Arguments.Add(Arguments);
            return n;
        }

        public override string ToString()
        {
            if (Arguments.Count == 0) return "{{" + Name + "}}";
            var sb = new StringBuilder("{{");
            var isFirst = true;
            sb.Append(Name);
            foreach (var arg in Arguments)
            {
                if (isFirst)
                {
                    sb.Append(IsMagicWord ? ':' : '|');
                    isFirst = false;
                }
                else
                {
                    sb.Append('|');
                }
                sb.Append(arg);
            }
            sb.Append("}}");
            return sb.ToString();
        }

        /// <inheritdoc />
        public override string ToPlainText(NodePlainTextOptions options)
        {
            return null;
        }
    }

    public class TemplateArgument : Node
    {
        private Wikitext _Name;
        private Wikitext _Value;

        public TemplateArgument() : this(null, null)
        {
        }

        public TemplateArgument(Wikitext name, Wikitext value)
        {
            Name = name;
            Value = value;
        }

        /// <summary>
        /// Name of the argument.
        /// </summary>
        /// <value>Name of the argument, or <c>null</c> if the argument is anonymous.</value>
        public Wikitext Name
        {
            get { return _Name; }
            set { Attach(ref _Name, value); }
        }

        /// <summary>
        /// Value of the argument.
        /// </summary>
        /// <value>Value of the argument. If the value is empty, it should be an empty <see cref="Wikitext"/> instance.</value>
        public Wikitext Value
        {
            get { return _Value; }
            set { Attach(ref _Value, value); }
        }

        /// <summary>
        /// Enumerates the children of this node.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override IEnumerable<Node> EnumChildren()
        {
            if (_Name != null) yield return _Name;
            if (_Value != null) yield return _Value;
        }

        protected override Node CloneCore()
        {
            var n = new TemplateArgument { Name = Name, Value = Value };
            return n;
        }

        public override string ToString()
        {
            if (Name == null) return Value.ToString();
            return Name + "=" + Value;
        }

        /// <summary>
        /// Infrastructure. This function will always throw a <seealso cref="NotSupportedException"/>.
        /// </summary>
        public override string ToPlainText(NodePlainTextOptions options)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// {{{name|defalut}}}
    /// </summary>
    public class ArgumentReference : InlineNode
    {
        private Wikitext _Name;
        private Wikitext _DefaultValue;

        public ArgumentReference() : this(null, null)
        {
        }

        public ArgumentReference(Wikitext name) : this(name, null)
        {
        }

        public ArgumentReference(Wikitext name, Wikitext defaultValue)
        {
            Name = name;
            DefaultValue = defaultValue;
        }

        /// <summary>
        /// Name of the argument.
        /// </summary>
        /// <value>Name of the argument.</value>
        public Wikitext Name
        {
            get { return _Name; }
            set { Attach(ref _Name, value); }
        }

        public Wikitext DefaultValue
        {
            get { return _DefaultValue; }
            set { Attach(ref _DefaultValue, value); }
        }

        /// <summary>
        /// Enumerates the children of this node.
        /// </summary>
        public override IEnumerable<Node> EnumChildren()
        {
            if (_Name != null) yield return _Name;
            if (_DefaultValue != null) yield return _DefaultValue;
        }

        protected override Node CloneCore()
        {
            var n = new ArgumentReference { Name = Name, DefaultValue = DefaultValue };
            return n;
        }

        public override string ToString()
        {
            var s = "{{{" + Name;
            if (DefaultValue != null) s += "|" + DefaultValue;
            return s + "}}}";
        }

        /// <inheritdoc />
        public override string ToPlainText(NodePlainTextOptions options)
        {
            return null;
        }
    }

    /// <summary>
    /// Determines how a tag is rendered in wikitext.
    /// </summary>
    public enum TagStyle
    {
        /// <summary>
        /// <c>&lt;tag&gt;&lt;/tag&gt;</c>
        /// </summary>
        Normal,

        /// <summary>
        /// <c>&lt;tag /&gt;</c>
        /// </summary> 
        SelfClosing,

        /// <summary>
        /// <c>&lt;tag&gt;</c>
        /// </summary>
        /// <remarks><see cref="CompactSelfClosing"/> and <see cref="NotClosed"/> have the same appearance in wikitext, but
        /// some tags, such as br, hr, and wbr, is always self-closed so &lt;br /&gt; should be recognized as a
        /// closed tag.</remarks>
        CompactSelfClosing,

        /// <summary>
        /// Unbalanced tags: <c>&lt;tag&gt;...[EOF]</c>.
        /// </summary>
        /// <remarks>
        /// <para><see cref="CompactSelfClosing"/> and <see cref="NotClosed"/> have the same appearance in wikitext, but
        /// the latter is for the tags that should be closed but actually not.</para>
        /// <para>MW parser forces to close all the unbalanced HTML tags at the end of the document. Unbalanced parser tags are escaped as plain text.</para>
        /// </remarks>
        NotClosed,
    }

    /// <summary>
    /// &lt;tag attr1=value1&gt;content&lt;/tag&gt;
    /// </summary>
    public abstract class TagNode : InlineNode
    {

        private string _ClosingTagTrailingWhitespace;
        private TagStyle _TagStyle;

        public TagNode() : this(null)
        {

        }

        public TagNode(string name)
        {
            Name = name;
            Attributes = new TagAttributeCollection(this);
        }

        /// <summary>
        /// Name of the tag.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Name of the closing tag. It may have a different letter-case from <see cref="Name"/>.
        /// </summary>
        /// <value>The name of closing tag. OR <c>null</c> if it shares exactly the same content as <see cref="Name"/>.</value>
        public string ClosingTagName { get; set; }

        /// <summary>
        /// How a tag is rendered in wikitext.
        /// </summary>
        public virtual TagStyle TagStyle
        {
            get { return _TagStyle; }
            set
            {
                if (value != TagStyle.Normal && value != TagStyle.SelfClosing
                    && value != TagStyle.CompactSelfClosing && value != TagStyle.NotClosed)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _TagStyle = value;
            }
        }

        /// <summary>
        /// The trailing whitespace for the closing tag.
        /// </summary>
        /// <exception cref="ArgumentException">The string contains non-white-space characters.</exception>
        public string ClosingTagTrailingWhitespace
        {
            get { return _ClosingTagTrailingWhitespace; }
            set
            {
                Utility.AssertNullOrWhiteSpace(value);
                _ClosingTagTrailingWhitespace = value;
            }
        }

        public TagAttributeCollection Attributes { get; }

        protected abstract string GetContentString();

        /// <summary>
        /// Gets the content inside the tags as plain text without the unprintable nodes
        /// (e.g. comments, templates).
        /// </summary>
        protected abstract string GetContentPlainText(NodePlainTextOptions options);

        /// <summary>
        /// Enumerates the children of this node.
        /// </summary>
        public override IEnumerable<Node> EnumChildren()
            => Attributes;

        /// <inheritdoc />
        public override string ToString()
        {
            var sb = new StringBuilder("<");
            sb.Append(Name);
            sb.Append(Attributes);
            switch (TagStyle)
            {
                case TagStyle.Normal:
                case TagStyle.NotClosed:
                    sb.Append('>');
                    sb.Append(GetContentString());
                    break;
                case TagStyle.SelfClosing:
                    sb.Append("/>");
                    return sb.ToString();
                case TagStyle.CompactSelfClosing:
                    sb.Append(">");
                    return sb.ToString();
                default:
                    Debug.Assert(false);
                    break;
            }
            if (TagStyle != TagStyle.NotClosed)
            {
                sb.Append("</");
                sb.Append(ClosingTagName ?? Name);
                sb.Append(ClosingTagTrailingWhitespace);
                sb.Append('>');
            }
            return sb.ToString();
        }

        /// <inheritdoc />
        public override string ToPlainText(NodePlainTextOptions options)
        {
            return GetContentPlainText(options);
        }
    }

    /// <summary>
    /// E.g. &lt;ref&gt;
    /// </summary>
    /// <remarks>
    /// The MediaWiki software adds elements that look and act like XML tags.
    /// Parser tags are included in MediaWiki whereas parser extension tags are added by optional software extensions.
    /// </remarks>
    public class ParserTag : TagNode
    {
        public ParserTag() : this(null)
        {

        }

        public ParserTag(string name) : base(name)
        {

        }

        /// <summary>
        /// Raw content of the tag.
        /// </summary>
        /// <value>Content of the tag, as string. If the tag is self-closing, the value is <c>null</c>.</value>
        public string Content { get; set; }

        protected override Node CloneCore()
        {
            var n = new ParserTag
            {
                Name = Name,
                ClosingTagName = ClosingTagName,
                Content = Content,
                ClosingTagTrailingWhitespace = ClosingTagTrailingWhitespace,
            };
            n.Attributes.Add(Attributes);
            n.Attributes.TrailingWhitespace = Attributes.TrailingWhitespace;
            return n;
        }

        /// <summary>
        /// Whether the tag is self closed. E.g. &lt;references /&gt;.
        /// </summary>
        public override TagStyle TagStyle
        {
            set
            {
                if (value == TagStyle.SelfClosing && value == TagStyle.CompactSelfClosing)
                {
                    if (!string.IsNullOrEmpty(Content))
                        throw new InvalidOperationException("Cannot self-close a tag with non-empty content.");
                }
                base.TagStyle = value;
            }
        }

        /// <inheritdoc />
        protected override string GetContentString() => Content;

        /// <inheritdoc />
        protected override string GetContentPlainText(NodePlainTextOptions options)
        {
            if ((options & NodePlainTextOptions.RemoveRefTags) == NodePlainTextOptions.RemoveRefTags
                && string.Equals(Name, "ref", StringComparison.OrdinalIgnoreCase))
                return null;
            return Content;
        }
    }

    /// <summary>
    /// Normal HTML tag, or other unrecognized tags. E.g. &lt;span&gt;
    /// </summary>
    /// <seealso cref="ParserTag"/>
    public class HtmlTag : TagNode
    {
        private Wikitext _Content;

        public HtmlTag() : this(null)
        {

        }

        public HtmlTag(string name) : base(name)
        {

        }

        /// <summary>
        /// Content of the tag.
        /// </summary>
        /// <value>Content of the tag, as <see cref="Wikitext"/>. If the tag is self-closing, the value is <c>null</c>.</value>
        public Wikitext Content
        {
            get { return _Content; }
            set { Attach(ref _Content, value); }
        }

        /// <summary>
        /// Enumerates the children of this node.
        /// </summary>
        public override IEnumerable<Node> EnumChildren()
        {
            foreach (var node in base.EnumChildren()) yield return node;
            if (Content != null) yield return Content;
        }

        protected override Node CloneCore()
        {
            var n = new HtmlTag
            {
                Name = Name,
                ClosingTagName = ClosingTagName,
                Content = Content,
                ClosingTagTrailingWhitespace = ClosingTagTrailingWhitespace,
            };
            n.Attributes.Add(Attributes);
            n.Attributes.TrailingWhitespace = Attributes.TrailingWhitespace;
            return n;
        }

        /// <summary>
        /// Whether the tag is self closed. E.g. &lt;references /&gt;.
        /// </summary>
        public override TagStyle TagStyle
        {
            set
            {
                if (value == TagStyle.SelfClosing && value == TagStyle.CompactSelfClosing)
                {
                    if (Content != null && Content.Lines.Count > 0)
                        throw new InvalidOperationException("Cannot self-close a tag with non-empty content.");
                }
                base.TagStyle = value;
            }
        }

        /// <inheritdoc />
        protected override string GetContentString() => Content?.ToString();

        /// <inheritdoc />
        protected override string GetContentPlainText(NodePlainTextOptions options) => Content?.ToPlainText(options);
    }

    /// <summary>
    /// Describes how the value of an attribute should be quoted.
    /// </summary>
    public enum ValueQuoteType
    {
        /// <summary>
        /// No quotes.
        /// </summary>
        None = 0,
        /// <summary>
        /// Value is surrounded by single quotes.
        /// </summary>
        SingleQuotes,
        /// <summary>
        /// Value is surrounded by double quotes.
        /// </summary>
        DoubleQuotes,
    }

    /// <summary>
    /// The attribute expression in a <see cref="TagNode"/>. E.g. <c>mode=traditional</c>.
    /// </summary>
    public class TagAttribute : Node
    {
        private string _LeadingWhitespace = " ";
        private string _WhitespaceBeforeEqualSign;
        private string _WhitespaceAfterEqualSign;
        private Run _Name;
        private Wikitext _Value;

        public Run Name
        {
            get { return _Name; }
            set { Attach(ref _Name, value); }
        }

        public Wikitext Value
        {
            get { return _Value; }
            set { Attach(ref _Value, value); }
        }

        /// <summary>
        /// How the value is quoted. If <see cref="Value"/> is <c>null</c>,
        /// this property is ignored.
        /// </summary>
        public ValueQuoteType Quote { get; set; }

        /// <summary>
        /// The whitespace before the property expression.
        /// </summary>
        /// <exception cref="ArgumentException">The string contains non-white-space characters. OR The string is <c>null</c> or empty.</exception>
        public string LeadingWhitespace
        {
            get { return _LeadingWhitespace; }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Only white space is accepted.", nameof(value));
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("Null or empty string is not accepted.", nameof(value));
                _LeadingWhitespace = value;
            }
        }

        /// <summary>
        /// The whitespace before equal sign.
        /// </summary>
        /// <exception cref="ArgumentException">The string contains non-white-space characters.</exception>
        public string WhitespaceBeforeEqualSign
        {
            get { return _WhitespaceBeforeEqualSign; }
            set
            {
                Utility.AssertNullOrWhiteSpace(value);
                _WhitespaceBeforeEqualSign = value;
            }
        }

        /// <summary>
        /// The whitespace after equal sign.
        /// </summary>
        /// <exception cref="ArgumentException">The string contains non-white-space characters.</exception>
        public string WhitespaceAfterEqualSign
        {
            get { return _WhitespaceAfterEqualSign; }
            set
            {
                Utility.AssertNullOrWhiteSpace(value);
                _WhitespaceAfterEqualSign = value;
            }
        }

        /// <summary>
        /// Enumerates the children of this node.
        /// </summary>
        public override IEnumerable<Node> EnumChildren()
        {
            if (_Name != null) yield return _Name;
            if (_Value != null) yield return _Value;
        }

        protected override Node CloneCore()
        {
            return new TagAttribute { Name = Name, Value = Value };
        }

        public override string ToString()
        {
            string quote;
            switch (Quote)
            {
                case ValueQuoteType.None:
                    quote = null;
                    break;
                case ValueQuoteType.SingleQuotes:
                    quote = "'";
                    break;
                case ValueQuoteType.DoubleQuotes:
                    quote = "\"";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return LeadingWhitespace + Name + WhitespaceBeforeEqualSign + "="
                   + WhitespaceAfterEqualSign + quote + Value + quote;
        }

        /// <inheritdoc />
        public override string ToPlainText(NodePlainTextOptions options)
        {
            throw new NotSupportedException();
        }
    }

    public class Comment : InlineNode
    {
        public Comment() : this(null)
        {
        }

        public Comment(string content)
        {
            Content = content;
        }

        public string Content { get; set; }

        /// <summary>
        /// Infrastructure. Enumerates the children of this node.
        /// </summary>
        /// <returns>Always an empty sequence of nodes.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override IEnumerable<Node> EnumChildren()
            => Enumerable.Empty<Node>();

        /// <inheritdoc />
        protected override Node CloneCore()
        {
            return new Comment { Content = Content };
        }

        /// <inheritdoc />
        public override string ToString() => "<!--" + Content + "-->";

        /// <inheritdoc />
        public override string ToPlainText(NodePlainTextOptions options) => null;
    }
}
