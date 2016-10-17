using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MwParserFromScratch.Nodes
{
    public class Wikitext : Node
    {
        public Wikitext()
        {
            Lines = new NodeCollection<LineNode>(this);
        }

        public NodeCollection<LineNode> Lines { get; }

        protected override Node CloneCore()
        {
            var n = new Wikitext();
            n.Lines.Add(Lines);
            return n;
        }

        public override string ToString() => string.Join("\n", Lines);
    }

    public abstract class InlineContainer : Node
    {
        /// <summary>
        /// Content of the inline container.
        /// </summary>
        public NodeCollection<InlineNode> Inlines { get; }

        public InlineContainer() : this(null)
        {
        }

        public InlineContainer(IEnumerable<InlineNode> nodes)
        {
            Inlines = new NodeCollection<InlineNode>(this);
            if (nodes != null) Inlines.Add(nodes);
        }

        /// <summary>
        /// Append a <see cref="PlainText"/> node to the end of the paragraph.
        /// </summary>
        /// <param name="text">The text to be inserted.</param>
        /// <returns>Either the new <see cref="PlainText"/> node inserted, or the existing <see cref="PlainText"/> in the end of the paragraph.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public PlainText Append(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            var pt = Inlines.LastNode as PlainText;
            if (pt == null) Inlines.Add(pt = new PlainText());
            pt.Content += text;
            return pt;
        }
    }

    /// <summary>
    /// A single-line RUN.
    /// </summary>
    public class Run : InlineContainer
    {
        public Run()
        {
        }

        public Run(params InlineNode[] nodes) : base(nodes)
        {
        }

        public Run(IEnumerable<InlineNode> nodes) : base(nodes)
        {
        }

        protected override Node CloneCore() => new Run(Inlines);

        public override string ToString()
        {
            return string.Join(null, Inlines);
        }
    }

    public abstract class LineNode : InlineContainer
    {
        public LineNode()
        {
        }

        public LineNode(IEnumerable<InlineNode> nodes) : base(nodes)
        {
        }
    }

    public class ListItem : LineNode
    {
        public ListItem()
        {
        }

        public ListItem(params InlineNode[] nodes) : base(nodes)
        {
        }

        public ListItem(IEnumerable<InlineNode> nodes) : base(nodes)
        {
        }

        /// <summary>
        /// Prefix of the item.
        /// </summary>
        /// <remarks>The prefix consists of one or more *#:;, or is simply a space. For HR, the prefix is at least 4 dashes.</remarks>
        public string Prefix { get; set; }

        protected override Node CloneCore()
        {
            var n = new ListItem(Inlines) {Prefix = Prefix};
            return n;
        }

        /// <summary>
        /// Gets the wikitext representation of the node.
        /// </summary>

        public override string ToString()
        {
            return Prefix + string.Join(null, Inlines);
        }
    }

    public class Heading : LineNode
    {
        private int _Level;

        public Heading()
        {
        }

        public Heading(params InlineNode[] nodes) : base(nodes)
        {
        }

        public Heading(IEnumerable<InlineNode> nodes) : base(nodes)
        {
        }


        /// <summary>
        /// Heading level.
        /// </summary>
        /// <value>
        /// The level of the heading, which equals to the number of
        /// equal signs (=) before or after the heading text.
        /// The value is between 1 and 6.
        /// </value>
        public int Level
        {
            get { return _Level; }
            set
            {
                if (value < 1 || value > 6)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _Level = value;
            }
        }

        protected override Node CloneCore()
        {
            var n = new Heading(Inlines) {Level = Level};
            return n;
        }

        public override string ToString()
        {
            var bar = new string('=', Level);
            return  bar + string.Join(null, Inlines) + bar;
        }
    }

    public class Paragraph : LineNode
    {
        public Paragraph()
        {
        }

        public Paragraph(params InlineNode[] nodes) : base(nodes)
        {
        }

        public Paragraph(IEnumerable<InlineNode> nodes) : base(nodes)
        {
        }

        private static readonly Regex ParagraphCloseMatcher = new Regex(@"\n\s*$");

        /// <summary>
        /// Whether to remove one trailing new-line, if possible.
        /// </summary>
        /// <remarks>
        /// <para>There should be 2 new-line characters (\n\n) after a paragraph. But if the next line
        /// is LIST_ITEM or HEADING, use 1 new-line character to end a paragraph is possible.</para>
        /// <para>For the last paragraph in the <see cref="Wikitext"/>, the expected number of new-line
        /// characters decreases by 1. That is, 1 for normal, 0 for compact.</para>
        /// <para>This property is <c>false</c> only if the last node of the paragraph is
        /// <see cref="PlainText"/>, and it ends with \n\s*. </para>
        /// </remarks>
        public bool Compact
        {
            get
            {
                var pt = Inlines.LastNode as PlainText;
                if (pt == null) return true;
                return !ParagraphCloseMatcher.IsMatch(pt.Content);
            }
        }

        protected override Node CloneCore()
        {
            var n = new Paragraph(Inlines);
            return n;
        }

        public override string ToString()
        {
            return string.Join(null, Inlines);
        }
    }
}
