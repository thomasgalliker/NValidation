namespace NValidation.Internals
{
    internal interface IPropertyRule<T>
    {
        string PropertyName { get; }

        /// <summary>
        /// null unless the property opted into a display name.
        /// </summary>
        Func<string>? DisplayName { get; }

        /// <summary>
        /// The groups this chain was put in, as declared — the default group included where it was named —
        /// or null for a chain declared without one, which is in the default group alone.
        /// </summary>
        string[]? Groups { get; }

        /// <summary>
        /// The name this chain's failures are reported under, which a selection of properties is matched
        /// against.
        /// </summary>
        string ReportedName { get; }

        /// <summary>
        /// Whether every rule in this chain judges rather than awaits, so the chain can be run through
        /// Validate without an async state machine. Settled while the chain is declared.
        /// </summary>
        bool IsSynchronous { get; }

        /// <summary>
        /// Runs this property's chain against one pass. The provider and the behaviour are resolved by the
        /// validator per pass; the chain only knows what it declared for itself.
        /// </summary>
        ValueTask ValidateAsync(ValidationFrame<T> frame, IValidationMessageProvider messageProvider, ValidationBehavior propertyBehavior);

        void Validate(ValidationFrame<T> frame, IValidationMessageProvider messageProvider, ValidationBehavior propertyBehavior);

        /// <summary>
        /// Runs only the rules of this chain that hand the value to another validator — one declaring a group
        /// of <paramref name="selection"/>, or any where it is null: what a selection leaving the default
        /// group out, or limited to properties below this one, reaches through this chain.
        /// </summary>
        ValueTask ValidateComposedAsync(
            ValidationFrame<T> frame, IValidationMessageProvider messageProvider, ValidationBehavior propertyBehavior, ValidationGroups? selection);

        void ValidateComposed(
            ValidationFrame<T> frame, IValidationMessageProvider messageProvider, ValidationBehavior propertyBehavior, ValidationGroups? selection);

        /// <summary>
        /// Marks the chain as in use: its checks are taken as an array, every later change is refused, and
        /// the display names of the validator's properties are what its messages resolve through.
        /// </summary>
        void Freeze(PropertyDisplayNames displayNames);

        /// <summary>
        /// The groups the validators this chain composes declare; null where it composes none that declares
        /// any. Asked once, by the freeze.
        /// </summary>
        string[]? GetComposedGroups();
    }
}
