namespace NValidation.Internals
{
    internal interface IPropertyRule<T>
    {
        string PropertyName { get; }

        // null unless the property opted into a display name.
        Func<string>? DisplayName { get; }

        // The groups this chain is in, or null for a chain in no group, which runs in every validation.
        string[]? Groups { get; }

        // Whether every rule in this chain judges rather than awaits, so the chain can be run through
        // Validate without an async state machine. Settled while the chain is declared.
        bool IsSynchronous { get; }

        // Runs this property's chain against one pass. The provider and the behaviour are resolved by the
        // validator per pass; the chain only knows what it declared for itself.
        ValueTask ValidateAsync(ValidationFrame<T> frame, IValidationMessageProvider messageProvider, ValidationBehavior propertyBehavior);

        void Validate(ValidationFrame<T> frame, IValidationMessageProvider messageProvider, ValidationBehavior propertyBehavior);

        // Marks the chain as in use: its checks are taken as an array, every later change is refused, and
        // the display names of the validator's properties are what its messages resolve through.
        void Freeze(PropertyDisplayNames displayNames);
    }
}
