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
    /// better without the property name; a provider which drops
    /// <see cref="ValidationMessagePlaceholders.PropertyName"/> from its templates gets that wording.
    /// </remarks>
    public sealed class DefaultValidationMessageProvider : IValidationMessageProvider
    {
        private static readonly FrozenDictionary<string, string> Messages = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ValidationErrorCodes.Must] = "{PropertyName} is not valid.",

            [ValidationErrorCodes.NotEmpty] = "{PropertyName} is required.",
            [ValidationErrorCodes.NotNull] = "{PropertyName} is required.",
            [ValidationErrorCodes.NotDefault] = "{PropertyName} is required.",
            [ValidationErrorCodes.NotNaN] = "{PropertyName} must be a number.",

            [ValidationErrorCodes.MinimumLength] = "{PropertyName} must be at least {MinLength} {MinLength:character|characters} long.",
            [ValidationErrorCodes.MaximumLength] = "{PropertyName} must not exceed {MaxLength} {MaxLength:character|characters}.",
            [ValidationErrorCodes.Length] = "{PropertyName} must be exactly {Length} {Length:character|characters} long.",
            [ValidationErrorCodes.LengthBetween] = "{PropertyName} must be between {MinLength} and {MaxLength} {MaxLength:character|characters} long.",
            [ValidationErrorCodes.Matches] = "{PropertyName} has an invalid format.",
            [ValidationErrorCodes.EmailAddress] = "{PropertyName} is not a valid email address.",
            [ValidationErrorCodes.EmailTopLevelDomain] = "{PropertyName} must use one of the following top-level domains: {TopLevelDomains}.",
            [ValidationErrorCodes.EmailTopLevelDomainNotAllowed] = "{PropertyName} must not use the top-level domain {TopLevelDomain}.",
            [ValidationErrorCodes.NotContaining] = "{PropertyName} contains text that is not allowed.",

            [ValidationErrorCodes.GreaterThan] = "{PropertyName} must be greater than {OtherValue}.",
            [ValidationErrorCodes.GreaterThanOrEqualTo] = "{PropertyName} must be greater than or equal to {OtherValue}.",
            [ValidationErrorCodes.LessThan] = "{PropertyName} must be less than {OtherValue}.",
            [ValidationErrorCodes.LessThanOrEqualTo] = "{PropertyName} must be less than or equal to {OtherValue}.",
            [ValidationErrorCodes.Between] = "{PropertyName} must be between {From} and {To}.",
            [ValidationErrorCodes.BetweenExclusive] = "{PropertyName} must be greater than {From} and less than {To}.",
            [ValidationErrorCodes.BetweenExclusiveFrom] = "{PropertyName} must be greater than {From} and at most {To}.",
            [ValidationErrorCodes.BetweenExclusiveTo] = "{PropertyName} must be at least {From} and less than {To}.",
            [ValidationErrorCodes.EqualTo] = "{PropertyName} must be {OtherValue}.",
            [ValidationErrorCodes.NotEqualTo] = "{PropertyName} must not be {OtherValue}.",

            [ValidationErrorCodes.GreaterThanOtherProperty] = "{PropertyName} must be greater than {OtherPropertyName}.",
            [ValidationErrorCodes.GreaterThanOrEqualToOtherProperty] = "{PropertyName} must be greater than or equal to {OtherPropertyName}.",
            [ValidationErrorCodes.LessThanOtherProperty] = "{PropertyName} must be less than {OtherPropertyName}.",
            [ValidationErrorCodes.LessThanOrEqualToOtherProperty] = "{PropertyName} must be less than or equal to {OtherPropertyName}.",
            [ValidationErrorCodes.EqualToOtherProperty] = "{PropertyName} must match {OtherPropertyName}.",
            [ValidationErrorCodes.NotEqualToOtherProperty] = "{PropertyName} must not match {OtherPropertyName}.",

            [ValidationErrorCodes.MultipleOf] = "{PropertyName} must be a multiple of {Step}.",
            [ValidationErrorCodes.PrecisionScale] = "{PropertyName} must not have more than {Precision} digits in total, with at most {Scale} after the decimal point.",
            [ValidationErrorCodes.OneOf] = "{PropertyName} must be one of: {AllowedValues}.",

            [ValidationErrorCodes.InThePast] = "{PropertyName} must be a date in the past.",
            [ValidationErrorCodes.InTheFuture] = "{PropertyName} must be a date in the future.",

            [ValidationErrorCodes.IsInEnum] = "{PropertyName} has an invalid value.",

            [ValidationErrorCodes.MinimumCount] = "{PropertyName} must contain at least {MinCount} {MinCount:entry|entries}.",
            [ValidationErrorCodes.MaximumCount] = "{PropertyName} must not contain more than {MaxCount} {MaxCount:entry|entries}.",
            [ValidationErrorCodes.NoDuplicates] = "{PropertyName} must not contain duplicate entries.",
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
        public string GetMessage(string errorCode, IReadOnlyDictionary<string, object?> arguments)
        {
            ArgumentNullException.ThrowIfNull(errorCode);

            var template = Messages.TryGetValue(errorCode, out var message) ? message : errorCode;
            return ValidationMessageFormatter.Format(template, arguments);
        }
    }
}
