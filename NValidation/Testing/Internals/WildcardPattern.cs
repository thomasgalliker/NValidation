namespace NValidation.Testing.Internals
{
    /// <summary>
    /// Matches an expected message against an actual one. <c>*</c> stands for any run of characters,
    /// <c>?</c> for exactly one, and <c>\*</c>, <c>\?</c> and <c>\\</c> for those characters themselves.
    /// The comparison is ordinal and case-sensitive, because a message which differs only in case
    /// differs.
    /// </summary>
    internal static class WildcardPattern
    {
        /// <summary>
        /// <c>true</c> when <paramref name="value"/> matches <paramref name="pattern"/>.
        /// </summary>
        public static bool IsMatch(string pattern, string value)
        {
            // A scan with one point of return: on a mismatch, go back to the last '*' seen and let it
            // swallow one more character. That is enough because a pattern has no alternatives — every
            // '*' is greedy and independent of the ones before it.
            var patternIndex = 0;
            var valueIndex = 0;
            var starIndex = -1;
            var valueIndexAtStar = 0;

            while (valueIndex < value.Length)
            {
                if (patternIndex < pattern.Length && IsSingleCharacterMatch(pattern, patternIndex, value[valueIndex]))
                {
                    patternIndex += TokenLength(pattern, patternIndex);
                    valueIndex++;
                }
                else if (patternIndex < pattern.Length && pattern[patternIndex] == '*')
                {
                    starIndex = patternIndex;
                    valueIndexAtStar = valueIndex;
                    patternIndex++;
                }
                else if (starIndex >= 0)
                {
                    patternIndex = starIndex + 1;
                    valueIndexAtStar++;
                    valueIndex = valueIndexAtStar;
                }
                else
                {
                    return false;
                }
            }

            while (patternIndex < pattern.Length && pattern[patternIndex] == '*')
            {
                patternIndex++;
            }

            return patternIndex == pattern.Length;
        }

        /// <summary>
        /// <c>true</c> when the token at <paramref name="patternIndex"/> matches exactly the one
        /// character <paramref name="candidate"/>.
        /// </summary>
        private static bool IsSingleCharacterMatch(string pattern, int patternIndex, char candidate)
        {
            if (pattern[patternIndex] == '?')
            {
                return true;
            }

            if (IsEscape(pattern, patternIndex))
            {
                return pattern[patternIndex + 1] == candidate;
            }

            return pattern[patternIndex] == candidate;
        }

        /// <summary>
        /// How many characters of the pattern the token at <paramref name="patternIndex"/> occupies:
        /// two for an escape, one otherwise.
        /// </summary>
        private static int TokenLength(string pattern, int patternIndex)
        {
            return IsEscape(pattern, patternIndex) ? 2 : 1;
        }

        /// <summary>
        /// A backslash which escapes a following <c>*</c>, <c>?</c> or <c>\</c>. A backslash before
        /// anything else is a plain backslash, so an ordinary message never has to be escaped.
        /// </summary>
        private static bool IsEscape(string pattern, int patternIndex)
        {
            return pattern[patternIndex] == '\\'
                && patternIndex + 1 < pattern.Length
                && (pattern[patternIndex + 1] == '*' || pattern[patternIndex + 1] == '?' || pattern[patternIndex + 1] == '\\');
        }
    }
}
