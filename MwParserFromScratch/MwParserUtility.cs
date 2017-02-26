using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MwParserFromScratch.Nodes;

namespace MwParserFromScratch
{
    /// <summary>
    /// Contains utility functions that might be handy for the wikitext parser.
    /// </summary>
    public static class MwParserUtility
    {
        /// <summary>
        /// Normalizes a template argument name.
        /// </summary>
        /// <param name="argumentName">The argument name to be normalized.</param>
        /// <returns>The normalized argument name, with leading and trailing whitespace removed.</returns>
        /// <exception cref="ArgumentNullException"><see cref="argumentName"/> is <c>null</c>.</exception>
        public static string NormalizeTemplateArgumentName(Node argumentName)
        {
            if (argumentName == null) throw new ArgumentNullException(nameof(argumentName));
            return NormalizeTemplateArgumentName(argumentName.ToString());
        }

        /// <summary>
        /// Normalizes a template argument name.
        /// </summary>
        /// <param name="argumentName">The argument name to be normalized.</param>
        /// <returns>The normalized argument name, with leading and trailing whitespace removed.</returns>
        /// <exception cref="ArgumentNullException"><see cref="argumentName"/> is <c>null</c>.</exception>
        public static string NormalizeTemplateArgumentName(string argumentName)
        {
            if (argumentName == null) throw new ArgumentNullException(nameof(argumentName));
            return argumentName.Trim();
        }

        /// <summary>
        /// Normalizes a page title expression. This is a simple version; it simply treats the part before
        /// the first colon mark as namesapce name. For a more complete version of title normalization,
        /// including title validation and namespace / interwiki prefix check,
        /// see WikiLink class in WikiClientLibrary package.
        /// </summary>
        /// <param name="title">The title to be normalized.</param>
        /// <returns>The normalized argument name, with leading and trailing whitespace removed,
        /// underscore replaced with space, starting with an upper-case letter.</returns>
        /// <exception cref="ArgumentNullException"><see cref="title"/> is <c>null</c>.</exception>
        public static string NormalizeTitle(Node title)
        {
            if (title == null) throw new ArgumentNullException(nameof(title));
            return NormalizeTitle(title.ToString());
        }

        /// <summary>
        /// Normalizes a page title expression. This is a simple version; it simply treats the part before
        /// the first colon mark as namesapce name. For a more complete version of title normalization,
        /// including title validation and namespace / interwiki prefix check,
        /// see WikiLink class in WikiClientLibrary package.
        /// </summary>
        /// <param name="title">The title to be normalized.</param>
        /// <returns>The normalized argument name, with leading and trailing whitespace removed,
        /// underscore replaced with space, starting with an upper-case letter.</returns>
        /// <exception cref="ArgumentNullException"><see cref="title"/> is <c>null</c>.</exception>
        public static string NormalizeTitle(string title)
        {
            if (title == null) throw new ArgumentNullException(nameof(title));
            var parts = title.Split(new[] {':'}, 2);
            for (int i = 0; i < parts.Length; i++)
                parts[i] = Utility.NormalizeTitlePart(parts[i], false);
            return string.Join(":", parts);
        }
    }
}
