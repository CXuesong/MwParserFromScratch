using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MwParserFromScratch.Nodes
{
    /// <summary>
    /// Represents wikitext with bold / italics.
    /// </summary>
    public class SimpleFormat : ContainerNode
    {
        /// <summary>
        /// Whether to switch font-bold for the content.
        /// </summary>
        public bool SwitchBold { get; set; }

        /// <summary>
        /// Whether to switch font-italics for the content.
        /// </summary>
        public bool SwitchItalics { get; set; }

        protected override Node CloneCore()
        {
            var n = new SimpleFormat {SwitchBold = SwitchBold, SwitchItalics = SwitchItalics};
            n.Add(Children);
            return n;
        }
    }

    public class Template : Node
    {
        public Wikitext Name { get; set; }

        protected override Node CloneCore()
        {
            throw new NotImplementedException();
        }
    }
}
