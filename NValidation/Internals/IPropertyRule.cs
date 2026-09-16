namespace NValidation.Internals
{
    internal interface IPropertyRule<in T>
    {
        string PropertyName { get; }

        /// <summary>
        /// <c>null</c> unless the property opted into a display name.
        /// </summary>
        Func<string>? DisplayName { get; }

        /// <summary>
        /// Runs this property's chain, reporting into the list the caller owns.
        /// </summary>
        /// <remarks>
        /// The behaviour is passed per call rather than read from the rule, because what a chain does
        /// once one of its rules has failed is resolved by the validator while validating — the rule
        /// only knows what it declared for itself.
        /// </remarks>
        ValueTask ValidateAsync(
            T instance,
            List<ValidationError> errors,
            IValidationMessageProvider messages,
            PropertyDisplayNames displayNames,
            ValidationBehavior propertyBehavior,
            RequestedBehaviors requested,
            CancellationToken cancellationToken);
    }
}
