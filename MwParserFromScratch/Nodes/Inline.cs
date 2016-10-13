using System;
using System.Collections.Generic;
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
        public string Content { get; set; }

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
            set { _Target = value == null ? null : Attach(value); }
        }

        public Run Text
        {
            get { return _Text; }
            set { _Text = value == null ? null : Attach(value); }
        }

        protected override Node CloneCore()
        {
            return new WikiLink {Target = Target, Text = Text};
        }

        public override string ToString() => $"[[{Target}|{Text}]]";
    }

    public class ExternalLink : InlineNode
    {
        private Run _Target;
        private Run _Text;

        public Run Target
        {
            get { return _Target; }
            set { _Target = value == null ? null : Attach(value); }
        }

        public Run Text
        {
            get { return _Text; }
            set { _Text = value == null ? null : Attach(value); }
        }

        protected override Node CloneCore()
        {
            return new ExternalLink { Target = Target, Text = Text };
        }

        public override string ToString() => $"[{Target} {Text}]";
    }

    /// <summary>
    /// Represents wikitext with bold / italics.
    /// </summary>
    public class SimpleFormat : InlineNode
    {
        private Run _Content;

        /// <summary>
        /// Whether to switch font-bold for the content.
        /// </summary>
        public bool SwitchBold { get; set; }

        /// <summary>
        /// Whether to switch font-italics for the content.
        /// </summary>
        public bool SwitchItalics { get; set; }

        public Run Content
        {
            get { return _Content; }
            set { _Content = value == null ? null : Attach(value); }
        }

        protected override Node CloneCore()
        {
            var n = new SimpleFormat {SwitchBold = SwitchBold, SwitchItalics = SwitchItalics, Content = Content};
            return n;
        }

        public override string ToString() => (SwitchBold ? "B" : "") + (SwitchItalics ? "I" : "") + $"[|{Content}|]";
    }

    public class Template : InlineNode
    {
        private Wikitext _Name;

        public Template()
        {
            Arguments = new NodeCollection<TemplateArgument>(this);
        }

        public Wikitext Name
        {
            get { return _Name; }
            set { _Name = Attach(value ?? new Wikitext()); }
        }

        public NodeCollection<TemplateArgument> Arguments { get; }

        protected override Node CloneCore()
        {
            var n = new Template {Name = Name};
            n.Arguments.Add(Arguments);
            return n;
        }

        public override string ToString() => $"{{{{ {Name} {string.Join("", Arguments)} }}}}";
    }

    public class TemplateArgument : Node
    {
        private Wikitext _Name;
        private Wikitext _Value;

        /// <summary>
        /// Name of the argument.
        /// </summary>
        /// <value>Name of the argument, or <c>null</c> if the argument is anonymous.</value>
        public Wikitext Name
        {
            get { return _Name; }
            set { _Name = Attach(value ?? new Wikitext()); }
        }

        public Wikitext Value
        {
            get { return _Value; }
            set { _Value = Attach(value ?? new Wikitext()); }
        }

        protected override Node CloneCore()
        {
            var n = new TemplateArgument {Name = Name, Value = Value};
            return n;
        }

        public override string ToString() => $" | {Name} = {Value}";
    }

    /// <summary>
    /// {{{name|defalut}}}
    /// </summary>
    public class ArgumentReference : Node
    {
        private Wikitext _Name;
        private Wikitext _DefaultValue;

        /// <summary>
        /// Name of the argument.
        /// </summary>
        /// <value>Name of the argument.</value>
        public Wikitext Name
        {
            get { return _Name; }
            set { _Name = Attach(value ?? new Wikitext()); }
        }

        public Wikitext DefaultValue
        {
            get { return _DefaultValue; }
            set { _DefaultValue = Attach(value ?? new Wikitext()); }
        }

        protected override Node CloneCore()
        {
            var n = new ArgumentReference { Name = Name, DefaultValue = DefaultValue };
            return n;
        }

        public override string ToString()
        {
            var s = "{{{ " + Name;
            if (DefaultValue != null) s += " | " + DefaultValue;
            return s + " }}}";
        }
    }

    /// <summary>
    /// &lt;tag attr1=value1&gt;content&lt;/tag&gt;
    /// </summary>
    public class Tag : Node
    {
        public Tag()
        {
            Attributes = new NodeCollection<TagAttribute>(this);
        }

        /// <summary>
        /// Name of the tag.
        /// </summary>
        public string Name { get; set; }

        public NodeCollection<TagAttribute> Attributes { get; }

        //TODO parse common tags, e.g. span
        /// <summary>
        /// Raw content of the tag.
        /// </summary>
        /// <value>Content of the tag, as string. If the tag is self-closing, the value is <c>null</c>.</value>
        public string RawContent { get; set; }

        protected override Node CloneCore()
        {
            var n = new Tag {Name = Name, RawContent = RawContent};
            n.Attributes.Add(Attributes);
            return n;
        }

        public override string ToString()
        {
            var s = $"<{Name}{string.Join("", Attributes)}";
            if (RawContent == null) return s + " />";
            return $"{s}>{RawContent}</{Name}>";
        }
    }

    public class TagAttribute : Node
    {
        public string Name { get; set; }

        public string Value { get; set; }

        protected override Node CloneCore()
        {
            return new TagAttribute {Name = Name, Value = Value};
        }

        public override string ToString() => $" {Name}={Value}";
    }

    public class Comment : Node
    {
        public string Content { get; set; }

        protected override Node CloneCore()
        {
            return new Comment {Content = Content};
        }

        public override string ToString() => $"C[|{Content}|]";
    }
}
