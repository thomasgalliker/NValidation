namespace NValidation.Internals
{
    internal static class GroupNames
    {
        public static readonly string[] Empty = [];

        public static string[] Copy(ReadOnlySpan<string> groups, string paramName)
        {
            foreach (var group in groups)
            {
                ThrowIfBlank(group, paramName);
            }

            return groups.ToArray();
        }

        public static void ThrowIfEmpty(ReadOnlySpan<string> groups, string paramName)
        {
            if (groups.Length == 0)
            {
                throw new ArgumentException("At least one group has to be named.", paramName);
            }
        }

        /// <summary>
        /// The groups a chain is already in plus the ones named now, in the order they were declared and
        /// without a repetition. Returns what it was given where nothing is new, so a chain declared inside
        /// a Group block shares that block's array rather than copying it per chain.
        /// </summary>
        public static string[] Union(string[]? existing, ReadOnlySpan<string> added, string paramName)
        {
            if (existing is null || existing.Length == 0)
            {
                return Copy(added, paramName);
            }

            List<string>? union = null;

            foreach (var group in added)
            {
                ThrowIfBlank(group, paramName);

                if (Contains(existing, group) || (union != null && union.Contains(group, StringComparer.Ordinal)))
                {
                    continue;
                }

                union ??= [.. existing];
                union.Add(group);
            }

            return union is null ? existing : [.. union];
        }

        public static bool Contains(string[] groups, string group)
        {
            for (var i = 0; i < groups.Length; i++)
            {
                if (string.Equals(groups[i], group, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ThrowIfBlank(string group, string paramName)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                throw new ArgumentException("A group name has to say something, so it cannot be empty.", paramName);
            }
        }
    }
}
