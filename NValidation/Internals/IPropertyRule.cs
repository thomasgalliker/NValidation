namespace NValidation.Internals
{
    internal interface IPropertyRule<T>
    {
        string PropertyName { get; }

        /// <summary>
        /// <c>null</c> unless the property opted into a display name.
        /// </summary>
        Func<string>? DisplayName { get; }

        /// <summary>
        /// Whether every rule in this chain judges rather than awaits, so the chain can be run through
        /// <see cref="Validate"/> without an async state machine. Settled while the chain is declared.
        /// </summary>
        bool IsSynchronous { get; }

        /// <summary>
        /// Runs this property's chain against one pass. The provider and the behaviour are resolved by
        /// the validator per pass; the chain only knows what it declared for itself.
        /// </summary>
        ValueTask ValidateAsync(ValidationFrame<T> frame, IValidationMessageProvider messageProvider, ValidationBehavior propertyBehavior);

        /// <inheritdoc cref="IsSynchronous"/>
        void Validate(ValidationFrame<T> frame, IValidationMessageProvider messageProvider, ValidationBehavior propertyBehavior);

        /// <summary>
        /// Marks the chain as in use: its checks are taken as an array, every later change is refused,
        /// and the display names of the validator's properties are what its messages resolve through.
        /// </summary>
        void Freeze(PropertyDisplayNames displayNames);
    }
}
