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
        /// <returns>The normalized argument name, with leading and trailing whitespace removed,
        /// or <c>null</c> if <see cref="argumentName"/> is <c>null</c>.</returns>
        public static string NormalizeTemplateArgumentName(Node argumentName)
        {
            if (argumentName == null) return null;
            return NormalizeTemplateArgumentName(argumentName.ToString());
        }

        /// <summary>
        /// Normalizes a template argument name.
        /// </summary>
        /// <param name="argumentName">The argument name to be normalized.</param>
        /// <returns>The normalized argument name, with leading and trailing whitespace removed,
        /// or <c>null</c> if <see cref="argumentName"/> is <c>null</c>.</returns>
        public static string NormalizeTemplateArgumentName(string argumentName)
        {
            if (string.IsNullOrEmpty(argumentName)) return argumentName;
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
        /// underscore replaced with space, starting with an upper-case letter.
        /// Or <c>null</c> if <see cref="title"/> is <c>null</c>.</returns>
        public static string NormalizeTitle(Node title)
        {
            if (title == null) return null;
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
        /// underscore replaced with space, starting with an upper-case letter.
        /// Or <c>null</c> if <see cref="title"/> is <c>null</c>.</returns>
        public static string NormalizeTitle(string title)
        {
            if (string.IsNullOrEmpty(title)) return title;
            var parts = title.Split(new[] {':'}, 2);
            for (int i = 0; i < parts.Length; i++)
                parts[i] = Utility.NormalizeTitlePart(parts[i], false);
            return string.Join(":", parts);
        }
    }
}
