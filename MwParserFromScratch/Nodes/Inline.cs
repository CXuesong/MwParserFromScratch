using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
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
            return new PlainText {Content = Content};
        }

        public override string ToString()
        {
            return Content;
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
            return new WikiLink {Target = Target, Text = Text};
        }

        public override string ToString() => Text == null ? $"[[{Target}]]" : $"[[{Target}|{Text}]]";
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
            var s = Target.ToString();
            if (Text != null) s += " " + Text;
            if (Brackets) s = "[" + s + "]";
            return s;
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
            var n = new FormatSwitch {SwitchBold = SwitchBold, SwitchItalics = SwitchItalics};
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
    }

    public class Template : InlineNode
    {
        private Run _Name;

        public Template() : this(null)
        {
        }

        public Template(Run name)
        {
            Name = name;
            Arguments = new NodeCollection<TemplateArgument>(this);
        }

        public Run Name
        {
            get { return _Name; }
            set { Attach(ref _Name, value); }
        }

        public NodeCollection<TemplateArgument> Arguments { get; }

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
            var n = new Template {Name = Name};
            n.Arguments.Add(Arguments);
            return n;
        }

        public override string ToString()
        {
            if (Arguments.Count == 0) return "{{" + Name + "}}";
            var sb = new StringBuilder("{{");
            sb.Append(Name);
            foreach (var arg in Arguments)
            {
                sb.Append('|');
                sb.Append(arg);
            }
            sb.Append("}}");
            return sb.ToString();
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
            var n = new TemplateArgument {Name = Name, Value = Value};
            return n;
        }

        public override string ToString()
        {
            if (Name == null) return Value.ToString();
            return Name + "=" + Value;
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
    }

    /// <summary>
    /// &lt;tag attr1=value1&gt;content&lt;/tag&gt;
    /// </summary>
    public abstract class TagNode : InlineNode
    {
        private string _TrailingWhitespace;

        public TagNode() : this(null)
        {
            
        }

        public TagNode(string name)
        {
            Name = name;
            Attributes = new NodeCollection<TagAttribute>(this);
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
        /// Whether the tag is self closing. E.g. &lt;references /&gt;.
        /// </summary>
        public abstract bool IsSelfClosing { get; set; }

        /// <summary>
        /// The trailing whitespace for the opening tag.
        /// </summary>
        /// <exception cref="ArgumentException">The string contains non-white-space characters.</exception>
        public string TrailingWhitespace
        {
            get { return _TrailingWhitespace; }
            set
            {
                Utility.AssertNullOrWhiteSpace(value);
                _TrailingWhitespace = value;
            }
        }

        /// <summary>
        /// The trailing whitespace for the closing tag.
        /// </summary>
        /// <exception cref="ArgumentException">The string contains non-white-space characters.</exception>
        public string ClosingTagTrailingWhitespace
        {
            get { return _TrailingWhitespace; }
            set
            {
                Utility.AssertNullOrWhiteSpace(value);
                _TrailingWhitespace = value;
            }
        }

        public NodeCollection<TagAttribute> Attributes { get; }

        protected abstract string GetContentString();

        /// <summary>
        /// Enumerates the children of this node.
        /// </summary>
        public override IEnumerable<Node> EnumChildren()
            => Attributes;

        public override string ToString()
        {
            var sb = new StringBuilder("<");
            sb.Append(Name);
            sb.Append(string.Join(null, Attributes));
            sb.Append(TrailingWhitespace);
            if (IsSelfClosing)
            {
                sb.Append("/>");
                return sb.ToString();
            }
            sb.Append('>');
            sb.Append(GetContentString());
            sb.Append("</");
            sb.Append(ClosingTagName ?? Name);
            sb.Append(ClosingTagTrailingWhitespace);
            sb.Append('>');
            return sb.ToString();
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
                TrailingWhitespace = TrailingWhitespace,
                ClosingTagTrailingWhitespace = ClosingTagTrailingWhitespace,
            };
            n.Attributes.Add(Attributes);
            return n;
        }

        /// <summary>
        /// Whether the tag is self closed. E.g. &lt;references /&gt;.
        /// </summary>
        public override bool IsSelfClosing
        {
            get { return Content == null; }
            set
            {
                if (value)
                {
                    if (!string.IsNullOrEmpty(Content))
                        throw new InvalidOperationException("Cannot self-close a tag with non-empty content.");
                    Content = null;
                }
                else if (Content == null)
                {
                    Content = "";
                }
            }
        }

        protected override string GetContentString() => Content;
    }

    public class HtmlTag : TagNode
    {
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
        public Wikitext Content { get; set; }

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
                TrailingWhitespace = TrailingWhitespace,
                ClosingTagTrailingWhitespace = ClosingTagTrailingWhitespace,
            };
            n.Attributes.Add(Attributes);
            return n;
        }

        /// <summary>
        /// Whether the tag is self closed. E.g. &lt;references /&gt;.
        /// </summary>
        public override bool IsSelfClosing
        {
            get { return Content == null; }
            set
            {
                if (value)
                {
                    if (Content != null && Content.Lines.Count > 0)
                        throw new InvalidOperationException("Cannot self-close a tag with non-empty content.");
                    Content = null;
                }
                else if (Content == null)
                {
                    Content = new Wikitext();
                }
            }
        }

        protected override string GetContentString() => Content?.ToString();
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
        /// Value is surrended by single quotes.
        /// </summary>
        SingleQuotes,
        /// <summary>
        /// Value is surrended by double quotes.
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
            return new TagAttribute {Name = Name, Value = Value};
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

        protected override Node CloneCore()
        {
            return new Comment {Content = Content};
        }

        public override string ToString() => "<!--" + Content + "-->";
    }
}
