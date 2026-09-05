namespace NValidation.Internals
{
    internal interface IPropertyRule<in T>
    {
        string Code { get; }

        /// <summary>
        /// <c>null</c> unless the property opted into a display name.
        /// </summary>
        Func<string>? DisplayName { get; }

        ValueTask ValidateAsync(
            T instance,
            List<ValidationError> errors,
            IValidationMessageProvider messages,
            PropertyDisplayNames displayNames,
            CancellationToken cancellationToken);

    }
}
