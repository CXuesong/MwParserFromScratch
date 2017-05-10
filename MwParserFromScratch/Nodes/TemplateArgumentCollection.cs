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

        /// <summary>
        /// Enumerates the normalized name-<see cref="TemplateArgument"/> pairs in the collection.
        /// </summary>
        /// <remarks>If there are arguments with duplicate names, they will nonetheless be included in the sequence.</remarks>
        public IEnumerable<KeyValuePair<string, TemplateArgument>> EnumNameArgumentPairs()
        {
            return EnumNameArgumentPairs(false);
        }

        private IEnumerable<KeyValuePair<string, TemplateArgument>> EnumNameArgumentPairs(bool reverse)
        {
            int unnamedCounter = reverse ? this.Count(arg => arg.Name == null) : 1; // for positional arguments
            foreach (var arg in reverse ? Reverse() : this)
            {
                if (arg.Name == null)
                {
                    yield return new KeyValuePair<string, TemplateArgument>(unnamedCounter.ToString(), arg);
                    if (reverse) unnamedCounter--;
                    else unnamedCounter++;
                }
                else
                {
                    yield return new KeyValuePair<string, TemplateArgument>(
                        MwParserUtility.NormalizeTemplateArgumentName(arg.Name), arg);
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
                return EnumNameArgumentPairs(true).FirstOrDefault(p => p.Key == name).Value;
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
            return EnumNameArgumentPairs(false).FirstOrDefault(p => p.Key == name).Value != null;
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

        /// <summary>
        /// Sets the value of the specified template argument. If the argument doesn't exist,
        /// this function will create a new one and returns it.
        /// </summary>
        /// <param name="argumentName">The name of the argument to set.</param>
        /// <param name="argumentValue">The new value of the argument. If the value is empty, it should be an empty <see cref="Wikitext"/> instance.</param>
        /// <returns>The <see cref="TemplateArgument"/> whose value has been set/created.</returns>
        /// <remarks>If there are multiple arguments sharing the same name, the value of the effective one (often the last one) will be set and returned.</remarks>
        /// <exception cref="ArgumentNullException">Either <paramref name="argumentName"/> or <paramref name="argumentValue"/> is <c>null</c>.</exception>
        public TemplateArgument SetValue(Wikitext argumentName, Wikitext argumentValue)
        {
            if (argumentName == null) throw new ArgumentNullException(nameof(argumentName));
            if (argumentValue == null) throw new ArgumentNullException(nameof(argumentValue));
            var arg = this[argumentName.ToString()];
            if (arg == null)
            {
                // TODO automatically convert named argument to positional one
                // E.g. {{T|1=abc}} --> {{T|abc}}
                arg = new TemplateArgument(argumentName, argumentValue);
                Add(arg);
            }
            else
            {
                arg.Value = argumentValue;
            }
            return arg;
        }
    }
}
