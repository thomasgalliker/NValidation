namespace NValidation.Internals
{
    internal static class Terms
    {
        // The entries a list-taking rule was given, or the ArgumentException a caller earns for a list
        // which could not describe anything.
        public static string[] Require(string[] values, string parameterName)
        {
            ArgumentNullException.ThrowIfNull(values, parameterName);

            if (values.Length == 0)
            {
                throw new ArgumentException("Name at least one entry; a rule with nothing to look for judges nothing.", parameterName);
            }

            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("A blank entry matches everything, which is never what was meant.", parameterName);
                }
            }

            return values;
        }

        // The entries without their leading dots and without repeats, in the order they were written, so a
        // message lists them as the caller did. A domain may be written with or without its dot, which is
        // why ".ch" and "ch" arrive here as the same entry.
        public static string[] ToDomains(string[] values, string parameterName)
        {
            var terms = Require(values, parameterName);
            var normalized = new List<string>(terms.Length);

            foreach (var term in terms)
            {
                var entry = term.TrimStart('.');

                if (entry.Length == 0)
                {
                    throw new ArgumentException("An entry of nothing but dots names no domain.", parameterName);
                }

                if (!normalized.Contains(entry, StringComparer.OrdinalIgnoreCase))
                {
                    normalized.Add(entry);
                }
            }

            return [.. normalized];
        }
    }
}
