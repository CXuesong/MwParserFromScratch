using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    public abstract class LineNode : Node
    {

    }

    public class ListItem : LineNode
    {
        private Run _Content;

        /// <summary>
        /// Prefix of the item.
        /// </summary>
        /// <remarks>The prefix consists of one or more *#:;, or is simply a space. For HR, the prefix is at least 4 dashes.</remarks>
        public string Prefix { get; set; }

        public Run Content
        {
            get { return _Content; }
            set { _Content = value == null ? null : Attach(value); }
        }

        protected override Node CloneCore()
        {
            var n = new ListItem {Prefix = Prefix, Content = Content};
            return n;
        }

        /// <summary>
        /// Gets the wikitext representation of the node.
        /// </summary>

        public override string ToString()
        {
            return $"{Prefix}[|{Content}|]";
        }
    }

    public class Heading : LineNode
    {
        private int _Level;
        private Run _Title;

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

        public Run Title
        {
            get { return _Title; }
            set { _Title = value == null ? null : Attach(value); }
        }

        protected override Node CloneCore()
        {
            var n = new Heading {Level = Level, Title = Title};
            return n;
        }

        public override string ToString()
        {
            return $"H{Level}[|{Title}|]";
        }
    }

    public class Paragraph : LineNode
    {
        public Paragraph()
        {
        }

        public Paragraph(Run content)
        {
            Content = content;
        }

        private Run _Content;

        public Run Content
        {
            get { return _Content; }
            set { _Content = value == null ? null : Attach(value); }
        }

        /// <summary>
        /// Whether to remove one trailing new-line, if possible.
        /// </summary>
        /// <remarks>
        /// <para>There should be 2 new-line characters (\n\n) after a paragraph. But if the next line
        /// is LIST_ITEM or HEADING, use 1 new-line character to end a paragraph is possible.</para>
        /// <para>For the last paragraph in the <see cref="Wikitext"/>, the expected number of new-line
        /// characters decreases by 1. That is, 1 for normal, 0 for compact.</para>
        /// </remarks>
        public bool Compact { get; set; }

        /// <summary>
        /// Append a <see cref="PlainText"/> node to the end of the paragraph.
        /// </summary>
        /// <param name="text">The text to be inserted.</param>
        /// <returns>Either the new <see cref="PlainText"/> node inserted, or the existing <see cref="PlainText"/> in the end of the paragraph.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public PlainText Append(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (Content == null) Content = new Run();
            var pt = Content.Inlines.LastNode as PlainText;
            if (pt == null) Content.Inlines.Add(pt = new PlainText());
            pt.Content += text;
            return pt;
        }

        /// <summary>
        /// Appends the children of <see cref="Run"/> to the end of the paragraph.
        /// </summary>
        /// <param name="run">The <see cref="Run"/> whose child will be copied and inserted.</param>
        /// <exception cref="ArgumentNullException"><paramref name="run"/> is <c>null</c>.</exception>

        public void Append(Run run)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            if (Content == null)
            {
                Content = (Run) run.Clone();
                return;
            }
            Content.Inlines.Add(run.Inlines);
        }

        protected override Node CloneCore()
        {
            var n = new Paragraph {Content = Content};
            return n;
        }

        public override string ToString()
        {
            return $"P{(Compact ? "C" : null)}[|{Content}|]";
        }
    }

    public class Run : Node
    {
        public Run() : this((IEnumerable<InlineNode>) null)
        {
        }

        public Run(params InlineNode[] nodes) : this((IEnumerable<InlineNode>) nodes)
        {
        }

        public Run(IEnumerable<InlineNode> nodes)
        {
            Inlines = new NodeCollection<InlineNode>(this);
            if (nodes != null) Inlines.Add(nodes);
        }

        public NodeCollection<InlineNode> Inlines { get; }

        protected override Node CloneCore() => new Run(Inlines);

        public override string ToString()
        {
            return string.Join("", Inlines);
        }
    }
}
