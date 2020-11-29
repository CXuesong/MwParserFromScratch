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

        /// <inheritdoc cref="NormalizeTemplateArgumentName(string)"/>
        /// <param name="argumentName">The argument name to be normalized. The node will be converted into its string representation.</param>
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
        /// or <c>null</c> if <paramref name="argumentName"/> is <c>null</c>.</returns>
        public static string NormalizeTemplateArgumentName(string argumentName)
        {
            if (string.IsNullOrEmpty(argumentName)) return argumentName;
            return argumentName.Trim();
        }

        /// <summary>
        /// Normalizes and manipulates a template argument name or value.
        /// </summary>
        /// <inheritdoc cref="NormalizeTemplateArgumentText(Wikitext)"/>
        /// <param name="text">The wikitext to be manipulated.</param>
        internal static void NormalizeTemplateArgumentText(Wikitext text)
        {
            if (text.Lines.First() is Paragraph firstParagraph)
            {
                if (firstParagraph.Inlines.First() is PlainText firstPlainText)
                    firstPlainText.Content = firstPlainText.Content.TrimStart();
            }

            if (text.Lines.Last() is Paragraph lastParagraph)
            {
                if (lastParagraph.Inlines.Last() is PlainText lastPlainText)
                    lastPlainText.Content = lastPlainText.Content.TrimEnd();
            }
        }

        /// <inheritdoc cref="NormalizeImageLinkArgumentName(string)"/>
        /// <param name="argumentName">The argument name to be normalized. The node will be converted into its string representation.</param>
        public static string NormalizeImageLinkArgumentName(Node argumentName)
        {
            if (argumentName == null) return null;
            return NormalizeImageLinkArgumentName(argumentName.ToString());
        }

        /// <summary>
        /// Normalizes the argument name used in Image link syntax.
        /// </summary>
        /// <param name="argumentName">The argument name to be normalized.</param>
        /// <returns>The normalized argument name, with leading and trailing whitespace removed, and first letter converted into lowercase
        /// or <c>null</c> if <paramref name="argumentName"/> is <c>null</c>.</returns>
        public static string NormalizeImageLinkArgumentName(string argumentName)
        {
            if (string.IsNullOrEmpty(argumentName)) return argumentName;
            argumentName = argumentName.Trim();
            if (argumentName.Length > 0 && char.IsUpper(argumentName, 0))
            {
                return char.ToLowerInvariant(argumentName[0]) + argumentName.Substring(1);
            }
            return argumentName;
        }


        /// <inheritdoc cref="NormalizeTitle(string)"/>
        /// <param name="title">The title to be normalized. The node will be converted into its string representation.</param>
        public static string NormalizeTitle(Node title)
        {
            if (title == null) return null;
            return NormalizeTitle(title.ToString());
        }

        /// <summary>
        /// Normalizes a page title expression. This is a simple version; it simply treats the part before
        /// the first colon mark as namespace name. For a more complete version of title normalization,
        /// including title validation and namespace / interwiki prefix check,
        /// see WikiLink class in WikiClientLibrary package.
        /// </summary>
        /// <param name="title">The title to be normalized.</param>
        /// <returns>The normalized argument name, with leading and trailing whitespace removed,
        /// underscore replaced with space, starting with an upper-case letter.
        /// Or <c>null</c> if <paramref name="title"/> is <c>null</c>.</returns>
        public static string NormalizeTitle(string title)
        {
            if (string.IsNullOrEmpty(title)) return title;
            var parts = title.Split(new[] { ':' }, 2);
            for (int i = 0; i < parts.Length; i++)
                parts[i] = Utility.NormalizeTitlePart(parts[i], false);
            return string.Join(":", parts);
        }
    }
}
