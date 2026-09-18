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
        /// Every error code the rules of this core can report, i.e. every constant of
        /// <see cref="ValidationErrorCodes"/>. Also the list of what a provider has to cover.
        /// </summary>
        /// <remarks>
        /// Written out rather than read by reflection: a trimmer is free to drop the metadata of constants
        /// that were inlined at their use sites, which would silently shorten the list in a published
        /// application.
        /// </remarks>
        public static IReadOnlyList<string> CoreErrorCodes()
        {
            return ErrorCodes;
        }

        /// <summary>
        /// Every placeholder name a rule of this core can supply, i.e. every constant of
        /// <see cref="ValidationMessagePlaceholders"/>.
        /// </summary>
        /// <inheritdoc cref="CoreErrorCodes" path="/remarks"/>
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
        /// Asserts that <paramref name="provider"/> has a message for <paramref name="errorCode"/> and
        /// that the message leaves no placeholder unresolved.
        /// </summary>
        /// <remarks>
        /// Does not require the message to name the failing property. A message shown underneath an
        /// already labelled input reads better without it, so whether to name it is the translation's
        /// call.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="provider"/> or <paramref name="errorCode"/> is <c>null</c>.</exception>
        /// <exception cref="ValidationAssertionException">The provider has no message for the key, or the message names a placeholder no rule supplies.</exception>
        public static void ShouldResolveErrorCode(this IValidationMessageProvider provider, string errorCode)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(errorCode);

            var message = provider.GetMessage(errorCode, CorePlaceholderArguments(PropertyNameStandIn));

            if (string.Equals(message, errorCode, StringComparison.Ordinal))
            {
                throw new ValidationAssertionException(
                    $"Expected the provider to have a message for \"{errorCode}\", but it answered with the key itself.");
            }

            var unresolved = FindUnresolvedPlaceholder(message);

            if (unresolved is not null)
            {
                throw new ValidationAssertionException(
                    $"Expected the message for \"{errorCode}\" to name only placeholders a rule supplies, " +
                    $"but \"{unresolved}\" was left in \"{message}\".");
            }
        }

        /// <summary>
        /// Asserts <see cref="ShouldResolveErrorCode"/> for every key of
        /// <see cref="CoreErrorCodes"/>, reporting all of the keys that fail rather than only the first.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="provider"/> is <c>null</c>.</exception>
        /// <exception cref="ValidationAssertionException">Any key fails.</exception>
        public static void ShouldResolveEveryCoreErrorCode(this IValidationMessageProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);

            var failures = new List<string>();

            foreach (var errorCode in ErrorCodes)
            {
                try
                {
                    provider.ShouldResolveErrorCode(errorCode);
                }
                catch (ValidationAssertionException exception)
                {
                    failures.Add(exception.Message);
                }
            }

            if (failures.Count > 0)
            {
                throw new ValidationAssertionException(
                    $"Expected the provider to answer for every error code of this core, but {failures.Count} of " +
                    $"{ErrorCodes.Length} failed:{Environment.NewLine}  " +
                    string.Join(Environment.NewLine + "  ", failures));
            }
        }

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
        /// Stands in for the failing property while a provider is being checked. Distinctive enough that it
        /// cannot collide with the wording of a message.
        /// </summary>
        private const string PropertyNameStandIn = "TheFailingProperty";

        private static readonly string[] ErrorCodes =
        [
            ValidationErrorCodes.Must,
            ValidationErrorCodes.NotEmpty,
            ValidationErrorCodes.NotNull,
            ValidationErrorCodes.NotDefault,
            ValidationErrorCodes.NotNaN,
            ValidationErrorCodes.MinimumLength,
            ValidationErrorCodes.MaximumLength,
            ValidationErrorCodes.Length,
            ValidationErrorCodes.LengthBetween,
            ValidationErrorCodes.Matches,
            ValidationErrorCodes.EmailAddress,
            ValidationErrorCodes.EmailTopLevelDomain,
            ValidationErrorCodes.EmailTopLevelDomainNotAllowed,
            ValidationErrorCodes.Url,
            ValidationErrorCodes.UrlScheme,
            ValidationErrorCodes.UrlHost,
            ValidationErrorCodes.NotContaining,
            ValidationErrorCodes.GreaterThan,
            ValidationErrorCodes.GreaterThanOrEqualTo,
            ValidationErrorCodes.LessThan,
            ValidationErrorCodes.LessThanOrEqualTo,
            ValidationErrorCodes.Between,
            ValidationErrorCodes.BetweenExclusive,
            ValidationErrorCodes.BetweenExclusiveFrom,
            ValidationErrorCodes.BetweenExclusiveTo,
            ValidationErrorCodes.EqualTo,
            ValidationErrorCodes.NotEqualTo,
            ValidationErrorCodes.GreaterThanOtherProperty,
            ValidationErrorCodes.GreaterThanOrEqualToOtherProperty,
            ValidationErrorCodes.LessThanOtherProperty,
            ValidationErrorCodes.LessThanOrEqualToOtherProperty,
            ValidationErrorCodes.EqualToOtherProperty,
            ValidationErrorCodes.NotEqualToOtherProperty,
            ValidationErrorCodes.MultipleOf,
            ValidationErrorCodes.PrecisionScale,
            ValidationErrorCodes.OneOf,
            ValidationErrorCodes.InThePast,
            ValidationErrorCodes.InTheFuture,
            ValidationErrorCodes.IsInEnum,
            ValidationErrorCodes.MinimumCount,
            ValidationErrorCodes.MaximumCount,
            ValidationErrorCodes.NoDuplicates,
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
            ValidationMessagePlaceholders.Precision,
            ValidationMessagePlaceholders.Scale,
            ValidationMessagePlaceholders.AllowedValues,
            ValidationMessagePlaceholders.MinCount,
            ValidationMessagePlaceholders.MaxCount,
            ValidationMessagePlaceholders.TopLevelDomains,
            ValidationMessagePlaceholders.TopLevelDomain,
            ValidationMessagePlaceholders.Schemes,
            ValidationMessagePlaceholders.Hosts,
            ValidationMessagePlaceholders.OtherValue,
            ValidationMessagePlaceholders.OtherPropertyName,
        ];
    }
}
