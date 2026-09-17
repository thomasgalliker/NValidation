namespace NValidation.Internals
{
    /// <summary>
    /// The settings a pass inherits: at the root, what the call was given; below it, what the composer
    /// resolved for itself. A member left <c>null</c> was not named at that level.
    /// </summary>
    /// <remarks>
    /// Two bytes rather than two <see cref="Nullable{T}"/>, because this travels inside every
    /// <see cref="ValidationRun"/> and sits on every <see cref="ValidationFrame"/>.
    /// </remarks>
    internal readonly struct InheritedSettings
    {
        private readonly byte classBehavior;

        private readonly byte propertyBehavior;

        public InheritedSettings(IValidationMessageProvider? messageProvider, ValidationBehavior? classBehavior, ValidationBehavior? propertyBehavior)
        {
            this.MessageProvider = messageProvider;
            this.classBehavior = Pack(classBehavior);
            this.propertyBehavior = Pack(propertyBehavior);
        }

        public IValidationMessageProvider? MessageProvider { get; }

        public ValidationBehavior? Class => Unpack(this.classBehavior);

        public ValidationBehavior? Property => Unpack(this.propertyBehavior);

        public static InheritedSettings From(NValidationOptions? options)
        {
            return options is null
                ? default
                : new InheritedSettings(options.MessageProvider, options.ValidationBehaviors.Class, options.ValidationBehaviors.Property);
        }

        /// <summary>
        /// The same as options, for a validator written by hand which can only take those; <c>null</c>
        /// where nothing is named.
        /// </summary>
        public NValidationOptions? AsOptions()
        {
            if (this.MessageProvider is null && this.classBehavior == 0 && this.propertyBehavior == 0)
            {
                return null;
            }

            return new NValidationOptions
            {
                MessageProvider = this.MessageProvider,
                ValidationBehaviors = new ValidationBehaviors { Class = this.Class, Property = this.Property },
            };
        }

        private static byte Pack(ValidationBehavior? behavior)
        {
            return behavior is { } value ? (byte)(value + 1) : (byte)0;
        }

        private static ValidationBehavior? Unpack(byte packed)
        {
            return packed == 0 ? null : (ValidationBehavior)(packed - 1);
        }
    }
}
