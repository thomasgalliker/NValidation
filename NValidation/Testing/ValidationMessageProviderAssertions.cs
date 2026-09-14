namespace NValidation.Testing
{
    /// <summary>
    /// Asserts that an <see cref="IValidationMessageProvider"/> can answer for every rule this core
    /// ships — the test an application writes once for its own provider, so a missing translation shows
    /// up in the suite rather than as a raw key in a response.
    /// </summary>
    public static class ValidationMessageProviderAssertions
    {
        /// <summary>
        /// Every message key the rules of this core can report, i.e. every constant of
        /// <see cref="ValidationMessageKeys"/>. Also the list of what a provider has to cover.
        /// </summary>
        /// <remarks>
        /// Written out rather than read by reflection: a trimmer is free to drop the metadata of constants
        /// that were inlined at their use sites, which would silently shorten the list in a published
        /// application.
        /// </remarks>
        public static IReadOnlyList<string> CoreMessageKeys()
        {
            return MessageKeys;
        }

        /// <summary>
        /// Every placeholder name a rule of this core can supply, i.e. every constant of
        /// <see cref="ValidationMessagePlaceholders"/>.
        /// </summary>
        /// <inheritdoc cref="CoreMessageKeys" path="/remarks"/>
        public static IReadOnlyList<string> CoreMessagePlaceholders()
        {
            return MessagePlaceholders;
        }

        /// <summary>
        /// An argument bag holding every placeholder of <see cref="CoreMessagePlaceholders"/>, with
        /// <paramref name="propertyName"/> as the failing property and each remaining placeholder standing
        /// for itself.
        /// </summary>
        /// <remarks>
        /// A message names any subset of the placeholders, so supplying all of them lets a test prove the
        /// opposite direction: that a text names nothing a rule does not actually supply.
        /// </remarks>
        public static IReadOnlyDictionary<string, object?> CorePlaceholderArguments(string propertyName)
        {
            ArgumentNullException.ThrowIfNull(propertyName);

            var arguments = new Dictionary<string, object?>(MessagePlaceholders.Length, StringComparer.Ordinal);

            foreach (var placeholder in MessagePlaceholders)
            {
                arguments[placeholder] = placeholder;
            }

            arguments[ValidationMessagePlaceholders.PropertyName] = propertyName;

            return arguments;
        }

        /// <summary>
        /// Asserts that <paramref name="provider"/> has a message for <paramref name="messageKey"/> and
        /// that the message leaves no placeholder unresolved.
        /// </summary>
        /// <remarks>
        /// Does not require the message to name the failing property. A message shown underneath an
        /// already labelled input reads better without it, so whether to name it is the translation's
        /// call.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="provider"/> or <paramref name="messageKey"/> is <c>null</c>.</exception>
        /// <exception cref="ValidationAssertionException">The provider has no message for the key, or the message names a placeholder no rule supplies.</exception>
        public static void ShouldResolveMessageKey(this IValidationMessageProvider provider, string messageKey)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(messageKey);

            var message = provider.GetMessage(messageKey, CorePlaceholderArguments(PropertyNameStandIn));

            if (string.Equals(message, messageKey, StringComparison.Ordinal))
            {
                throw new ValidationAssertionException(
                    $"Expected the provider to have a message for \"{messageKey}\", but it answered with the key itself.");
            }

            var unresolved = FindUnresolvedPlaceholder(message);

            if (unresolved is not null)
            {
                throw new ValidationAssertionException(
                    $"Expected the message for \"{messageKey}\" to name only placeholders a rule supplies, " +
                    $"but \"{unresolved}\" was left in \"{message}\".");
            }
        }

        /// <summary>
        /// Asserts <see cref="ShouldResolveMessageKey"/> for every key of
        /// <see cref="CoreMessageKeys"/>, reporting all of the keys that fail rather than only the first.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="provider"/> is <c>null</c>.</exception>
        /// <exception cref="ValidationAssertionException">Any key fails.</exception>
        public static void ShouldResolveEveryCoreMessageKey(this IValidationMessageProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);

            var failures = new List<string>();

            foreach (var messageKey in MessageKeys)
            {
                try
                {
                    provider.ShouldResolveMessageKey(messageKey);
                }
                catch (ValidationAssertionException exception)
                {
                    failures.Add(exception.Message);
                }
            }

            if (failures.Count > 0)
            {
                throw new ValidationAssertionException(
                    $"Expected the provider to answer for every message key of this core, but {failures.Count} of " +
                    $"{MessageKeys.Length} failed:{Environment.NewLine}  " +
                    string.Join(Environment.NewLine + "  ", failures));
            }
        }

        /// <summary>
        /// The first <c>{Placeholder}</c> or <c>{Placeholder:format}</c> still in <paramref name="message"/>,
        /// or <c>null</c> when the arguments covered all of them.
        /// </summary>
        private static string? FindUnresolvedPlaceholder(string message)
        {
            var openingBrace = message.IndexOf('{', StringComparison.Ordinal);

            while (openingBrace >= 0)
            {
                var closingBrace = message.IndexOf('}', openingBrace + 1);

                if (closingBrace < 0)
                {
                    return null;
                }

                var token = message.AsSpan(openingBrace + 1, closingBrace - openingBrace - 1);

                if (LooksLikeAPlaceholder(token))
                {
                    return message.Substring(openingBrace, closingBrace - openingBrace + 1);
                }

                openingBrace = message.IndexOf('{', closingBrace + 1);
            }

            return null;
        }

        /// <summary>
        /// A name of word characters, optionally followed by <c>:</c> and a format — which is the shape
        /// <see cref="ValidationMessageFormatter"/> substitutes. Anything else inside braces is text.
        /// </summary>
        private static bool LooksLikeAPlaceholder(ReadOnlySpan<char> token)
        {
            var name = token;
            var colon = token.IndexOf(':');

            if (colon >= 0)
            {
                name = token[..colon];

                if (colon == token.Length - 1)
                {
                    return false;
                }
            }

            if (name.IsEmpty)
            {
                return false;
            }

            foreach (var character in name)
            {
                if (!char.IsLetterOrDigit(character) && character != '_')
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Stands in for the failing property while a provider is being checked. Distinctive enough that
        /// it cannot collide with the wording of a message.
        /// </summary>
        private const string PropertyNameStandIn = "TheFailingProperty";

        private static readonly string[] MessageKeys =
        [
            ValidationMessageKeys.NotEmpty,
            ValidationMessageKeys.NotNull,
            ValidationMessageKeys.NotDefault,
            ValidationMessageKeys.NotNaN,
            ValidationMessageKeys.MinimumLength,
            ValidationMessageKeys.MaximumLength,
            ValidationMessageKeys.Length,
            ValidationMessageKeys.LengthBetween,
            ValidationMessageKeys.Matches,
            ValidationMessageKeys.EmailAddress,
            ValidationMessageKeys.EmailTopLevelDomain,
            ValidationMessageKeys.EmailTopLevelDomainNotAllowed,
            ValidationMessageKeys.NotContaining,
            ValidationMessageKeys.GreaterThan,
            ValidationMessageKeys.GreaterThanOrEqualTo,
            ValidationMessageKeys.LessThan,
            ValidationMessageKeys.LessThanOrEqualTo,
            ValidationMessageKeys.Between,
            ValidationMessageKeys.EqualTo,
            ValidationMessageKeys.NotEqualTo,
            ValidationMessageKeys.GreaterThanOtherProperty,
            ValidationMessageKeys.GreaterThanOrEqualToOtherProperty,
            ValidationMessageKeys.LessThanOtherProperty,
            ValidationMessageKeys.LessThanOrEqualToOtherProperty,
            ValidationMessageKeys.EqualToOtherProperty,
            ValidationMessageKeys.NotEqualToOtherProperty,
            ValidationMessageKeys.MultipleOf,
            ValidationMessageKeys.InThePast,
            ValidationMessageKeys.InTheFuture,
            ValidationMessageKeys.IsInEnum,
            ValidationMessageKeys.MinimumCount,
            ValidationMessageKeys.MaximumCount,
            ValidationMessageKeys.NoDuplicates,
        ];

        private static readonly string[] MessagePlaceholders =
        [
            ValidationMessagePlaceholders.PropertyName,
            ValidationMessagePlaceholders.CollectionIndex,
            ValidationMessagePlaceholders.MinLength,
            ValidationMessagePlaceholders.MaxLength,
            ValidationMessagePlaceholders.Length,
            ValidationMessagePlaceholders.Pattern,
            ValidationMessagePlaceholders.From,
            ValidationMessagePlaceholders.To,
            ValidationMessagePlaceholders.Step,
            ValidationMessagePlaceholders.MinCount,
            ValidationMessagePlaceholders.MaxCount,
            ValidationMessagePlaceholders.TopLevelDomains,
            ValidationMessagePlaceholders.TopLevelDomain,
            ValidationMessagePlaceholders.OtherValue,
            ValidationMessagePlaceholders.OtherPropertyName,
        ];
    }
}
