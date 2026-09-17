namespace NValidation.Testing.Internals
{
    /// <summary>
    /// Matches a pattern of <c>*</c> and <c>?</c> against a value, with <c>\</c> escaping either.
    /// </summary>
    internal static class WildcardPattern
    {
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
