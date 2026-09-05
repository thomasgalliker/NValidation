using System.Collections.Frozen;

namespace NValidation
{
    /// <summary>
    /// The English fallback, so the core is usable on its own. A host which cares about wording or
    /// other languages supplies its own <see cref="IValidationMessageProvider"/> instead.
    /// </summary>
    /// <remarks>
    /// These texts name the property, which suits a message read on its own — a log line, or a client
    /// with no form to attach it to. A message rendered underneath an already labelled input reads
    /// better without the name; a provider which drops
    /// <see cref="ValidationMessagePlaceholders.PropertyName"/> from its templates gets that wording.
    /// </remarks>
    public sealed class DefaultValidationMessageProvider : IValidationMessageProvider
    {
        private static readonly FrozenDictionary<string, string> Messages = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ValidationMessageKeys.NotEmpty] = "{PropertyName} is required.",
            [ValidationMessageKeys.NotNull] = "{PropertyName} is required.",
            [ValidationMessageKeys.NotDefault] = "{PropertyName} is required.",
            [ValidationMessageKeys.NotNaN] = "{PropertyName} must be a number.",

            [ValidationMessageKeys.MinimumLength] = "{PropertyName} must be at least {MinLength} characters long.",
            [ValidationMessageKeys.MaximumLength] = "{PropertyName} must not exceed {MaxLength} characters.",
            [ValidationMessageKeys.Length] = "{PropertyName} must be exactly {Length} characters long.",
            [ValidationMessageKeys.LengthBetween] = "{PropertyName} must be between {MinLength} and {MaxLength} characters long.",
            [ValidationMessageKeys.Matches] = "{PropertyName} has an invalid format.",
            [ValidationMessageKeys.EmailAddress] = "{PropertyName} is not a valid email address.",

            [ValidationMessageKeys.GreaterThan] = "{PropertyName} must be greater than {OtherValue}.",
            [ValidationMessageKeys.GreaterThanOrEqualTo] = "{PropertyName} must be greater than or equal to {OtherValue}.",
            [ValidationMessageKeys.LessThan] = "{PropertyName} must be less than {OtherValue}.",
            [ValidationMessageKeys.LessThanOrEqualTo] = "{PropertyName} must be less than or equal to {OtherValue}.",
            [ValidationMessageKeys.Between] = "{PropertyName} must be between {From} and {To}.",
            [ValidationMessageKeys.EqualTo] = "{PropertyName} must be {OtherValue}.",
            [ValidationMessageKeys.NotEqualTo] = "{PropertyName} must not be {OtherValue}.",

            [ValidationMessageKeys.GreaterThanOtherProperty] = "{PropertyName} must be greater than {OtherPropertyName}.",
            [ValidationMessageKeys.GreaterThanOrEqualToOtherProperty] = "{PropertyName} must be greater than or equal to {OtherPropertyName}.",
            [ValidationMessageKeys.LessThanOtherProperty] = "{PropertyName} must be less than {OtherPropertyName}.",
            [ValidationMessageKeys.LessThanOrEqualToOtherProperty] = "{PropertyName} must be less than or equal to {OtherPropertyName}.",
            [ValidationMessageKeys.EqualToOtherProperty] = "{PropertyName} must match {OtherPropertyName}.",
            [ValidationMessageKeys.NotEqualToOtherProperty] = "{PropertyName} must not match {OtherPropertyName}.",

            [ValidationMessageKeys.MultipleOf] = "{PropertyName} must be a multiple of {Step}.",

            [ValidationMessageKeys.InThePast] = "{PropertyName} must be a date in the past.",
            [ValidationMessageKeys.InTheFuture] = "{PropertyName} must be a date in the future.",

            [ValidationMessageKeys.IsInEnum] = "{PropertyName} has an invalid value.",

            [ValidationMessageKeys.MinimumCount] = "{PropertyName} must contain at least {MinCount} entries.",
            [ValidationMessageKeys.MaximumCount] = "{PropertyName} must not contain more than {MaxCount} entries.",
            [ValidationMessageKeys.NoDuplicates] = "{PropertyName} must not contain duplicate entries.",
        }.ToFrozenDictionary(StringComparer.Ordinal);

        /// <summary>
        /// The shared instance. It holds no state, so there is nothing to gain from a second one.
        /// </summary>
        public static DefaultValidationMessageProvider Instance { get; } = new DefaultValidationMessageProvider();

        /// <inheritdoc/>
        /// <remarks>
        /// A key with no built-in message resolves to the key itself, so an unmapped rule still produces
        /// something readable rather than failing the call.
        /// </remarks>
        public string GetMessage(string messageKey, IReadOnlyDictionary<string, object?> arguments)
        {
            ArgumentNullException.ThrowIfNull(messageKey);

            var template = Messages.TryGetValue(messageKey, out var message) ? message : messageKey;
            return ValidationMessageFormatter.Format(template, arguments);
        }
    }
}
