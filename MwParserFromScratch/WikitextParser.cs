using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MwParserFromScratch.Nodes;

namespace MwParserFromScratch
{
    public class WikitextParser
    {
        private string fulltext;
        private int position;        // Starting index of the string to be consumed.

        public IEnumerable<Wikitext> Parse(string wikitext)
        {
            throw new NotImplementedException();
            if (wikitext == null) throw new ArgumentNullException(nameof(wikitext));
            var pos = 0;
            this.fulltext = wikitext;
            while (pos < wikitext.Length)
            {

            }
            return null;
        }
    }
}
