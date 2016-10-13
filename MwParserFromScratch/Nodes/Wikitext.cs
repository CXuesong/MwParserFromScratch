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
        /// <remarks>The prefix consists of one or more *#:;, or is simply a space.</remarks>
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
        private InlineNode _Title;

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

        public InlineNode Title
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

    public class HorizontalRuler : LineNode
    {
        protected override Node CloneCore()
        {
            var n = new HorizontalRuler();
            return n;
        }

        public override string ToString()
        {
            return "HR";
        }
    }

    public class Paragraph : LineNode
    {
        private Run _Content;

        public Run Content
        {
            get { return _Content; }
            set { _Content = value == null ? null : Attach(value); }
        }

        protected override Node CloneCore()
        {
            var n = new Paragraph {Content = Content};
            return n;
        }

        public override string ToString()
        {
            return $"P[|{Content}|]";
        }
    }

    public class Run : Node
    {
        public Run()
        {
            Inlines = new NodeCollection<InlineNode>(this);
        }

        public NodeCollection<InlineNode> Inlines { get; }

        protected override Node CloneCore()
        {
            var n = new Run();
            n.Inlines.Add(Inlines);
            return n;
        }

        public override string ToString()
        {
            return string.Join(" ", Inlines);
        }
    }
}
