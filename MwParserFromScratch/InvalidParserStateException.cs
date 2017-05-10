using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if NET45
using System.Runtime.Serialization;
#endif

namespace MwParserFromScratch
{
    /// <summary>
    /// Represents the parsing state of the parser is invalid. This exception indicates
    /// that there might be a b&#117;g with the parser.  
    /// </summary>
    /// <remarks>
    /// When you come across this exception, you may create an issue on this project's GitHub page,
    /// attaching the original wikitext that suffered from this Exception, and the partial
    /// call stack that raises the exception.
    /// </remarks>
#if NET45
        [Serializable]
#endif
    public class InvalidParserStateException : Exception
    {
        public InvalidParserStateException()
            : base("The parser state is invalid. There might be a bug with the parser.")
        {
        }

        public InvalidParserStateException(string message) : base(message)
        {
        }

        public InvalidParserStateException(string message, Exception inner) : base(message, inner)
        {
        }

#if NET45
        public InvalidParserStateException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            
        }
#endif
    }
}
