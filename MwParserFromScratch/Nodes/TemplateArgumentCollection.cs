using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MwParserFromScratch.Nodes
{
    /// <summary>
    /// Represents a collection of <see cref="TemplateArgument"/>,
    /// which can be accessed via argument names.
    /// </summary>
    public class TemplateArgumentCollection : NodeCollection<TemplateArgument>
    {

        internal TemplateArgumentCollection(Node owner) : base(owner)
        {

        }

        private IEnumerable<KeyValuePair<string, TemplateArgument>> EnumNameArgumentPairs()
        {
            int index = 1;      // for positional arguments
            foreach (var arg in this)
            {
                if (arg.Name == null)
                {
                    yield return new KeyValuePair<string, TemplateArgument>(index.ToString(), arg);
                    index++;
                }
                else
                {
                    yield return new KeyValuePair<string, TemplateArgument>(MwParserUtility.NormalizeTemplateArgumentName(arg.Name), arg);
                }
            }
        }

        /// <summary>
        /// Gets an argument with the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of argument that will be tested. Can either be a name or 1-based index.
        /// Leading and trailing white spaces will be ignored.
        /// </param>
        /// <exception cref="ArgumentNullException"><see cref="name"/> is <c>null</c>.</exception>
        /// <returns>A matching <see cref="TemplateArgument"/> with the specified name, or <c>null</c> if no matching template is found.</returns>
        public TemplateArgument this[string name]
        {
            get
            {
                if (name == null) throw new ArgumentNullException(nameof(name));
                name = name.Trim();
                // We want to choose the last matching arguments, if there're multiple choices.
                return EnumNameArgumentPairs().Last(p => p.Key == name).Value;
            }
        }

        /// <summary>
        /// Gets an argument with the specified positional argument index.
        /// </summary>
        /// <param name="name">
        /// The index of argument that will be tested. Note that this index will not nessarily greater
        /// or equal than 1, because there might exist template argument with the name such as "-1", which
        /// can still be matched using this accessor.
        /// </param>
        /// <returns>A matching <see cref="TemplateArgument"/> with the specified name, or <c>null</c> if no matching template is found.</returns>
        /// <exception cref="ArgumentNullException"><see cref="name"/> is <c>null</c>.</exception>
        public TemplateArgument this[int name] => this[name.ToString()];

        /// <summary>
        /// Determines whether an argument with the specified name exists.
        /// </summary>
        /// <param name="name">
        /// The name of argument that will be tested. Can either be a name or 1-based index.
        /// Leading and trailing white spaces will be ignored.
        /// </param>
        /// <exception cref="ArgumentNullException"><see cref="name"/> is <c>null</c>.</exception>
        public bool Contains(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            name = name.Trim();
            return EnumNameArgumentPairs().Last(p => p.Key == name).Value != null;
        }

        /// <summary>
        /// Determines whether an argument with the specified positional argument index exists.
        /// </summary>
        /// <param name="name">
        /// The index of argument that will be tested. Note that this index will not nessarily greater
        /// or equal than 1, because there might exist template argument with the name such as "-1", which
        /// can still be matched using this accessor.
        /// </param>
        /// <exception cref="ArgumentNullException"><see cref="name"/> is <c>null</c>.</exception>
        public bool Contains(int name) => Contains(name.ToString());
    }
}
