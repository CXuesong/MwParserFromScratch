using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MwParserFromScratch.Nodes
{
    [ChildrenType(typeof(ListItem))]
    [ChildrenType(typeof(Heading))]
    [ChildrenType(typeof(HorizontalRuler))]
    [ChildrenType(typeof(Paragraph))]
    public class Wikitext : ContainerNode
    {
        protected override Node CloneCore()
        {
            var n = new Wikitext();
            n.Add(Children);
            return n;
        }
    }

    public class ListItem : ContainerNode
    {
        /// <summary>
        /// Prefix of the item.
        /// </summary>
        /// <remarks>The prefix consists of one or more *#:;, or is simply a space.</remarks>
        public string Prefix { get; set; }

        protected override Node CloneCore()
        {
            var n = new ListItem {Prefix = Prefix};
            n.Add(Children);
            return n;
        }
    }

    public class Heading : ContainerNode
    {
        private int _Level;

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
            var n = new Heading {Level = Level};
            n.Add(Children);
            return n;
        }
    }

    public class HorizontalRuler : Node
    {
        protected override Node CloneCore()
        {
            var n = new HorizontalRuler();
            return n;
        }
    }

    public class Paragraph : ContainerNode
    {
        protected override Node CloneCore()
        {
            var n = new Paragraph();
            n.Add(Children);
            return n;
        }
    }
}
