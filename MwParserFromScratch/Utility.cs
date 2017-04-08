using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MwParserFromScratch
{
    internal static class Utility
    {
        public static T PeekOrDefault<T>(this Stack<T> stack)
        {
            if (stack == null) throw new ArgumentNullException(nameof(stack));
            if (stack.Count == 0) return default(T);
            return stack.Peek();
        }

        public static TValue TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
        {
            if (dict == null) throw new ArgumentNullException(nameof(dict));
            TValue v;
            if (dict.TryGetValue(key, out v)) return v;
            return default(TValue);
        }

        public static void AssertNullOrWhiteSpace(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Argument is neither null or white space.", nameof(value));
        }

        public static IEnumerable<T> Singleton<T>(T value)
        {
            yield return value;
        }

        /// <summary>
        /// Normalizes part of title (either namespace name or page title, not both)
        /// to its cannonical form. (Copied from WikiClientLibrary.)
        /// </summary>
        /// <param name="title">The title to be normalized.</param>
        /// <param name="caseSensitive">Whether the title is case sensitive.</param>
        /// <returns>
        /// Normalized part of title. The underscores are replaced by spaces,
        /// and when <paramref name="caseSensitive"/> is <c>true</c>, the first letter is
        /// upper-case. Multiple spaces will be replaced with a single space. Lading
        /// and trailing spaces will be removed.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="title"/> is <c>null</c>.</exception>
        public static string NormalizeTitlePart(string title, bool caseSensitive)
        {
            // Reference to Pywikibot. page.py, Link class.
            if (title == null) throw new ArgumentNullException(nameof(title));
            if (title.Length == 0) return title;
            var state = 0;
            /*
             STATE
             0      Leading whitespace
             1      In title, after non-whitespace
             2      In title, after whitespace
             */
            var sb = new StringBuilder();
            foreach (var c in title)
            {
                var isWhitespace = c == '_' || char.IsWhiteSpace(c);
                // Remove left-to-right and right-to-left markers.
                if (c == '\u200e' || c == '\u200f') continue;
                switch (state)
                {
                    case 0:
                        if (!isWhitespace)
                        {
                            sb.Append(caseSensitive ? c : char.ToUpperInvariant(c));
                            state = 1;
                        }
                        break;
                    case 1:
                        if (isWhitespace)
                        {
                            sb.Append(' ');
                            state = 2;
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                    case 2:
                        if (!isWhitespace)
                        {
                            sb.Append(c);
                            state = 1;
                        }
                        break;
                }
            }
            if (state == 2)
            {
                // Remove trailing space.
                Debug.Assert(sb[sb.Length - 1] == ' ');
                return sb.ToString(0, sb.Length - 1);
            }
            return sb.ToString();
        }
    }
}
