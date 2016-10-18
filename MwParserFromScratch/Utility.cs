using System;
using System.Collections.Generic;
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
    }
}
