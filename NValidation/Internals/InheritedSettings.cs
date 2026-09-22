namespace NValidation.Internals
{
    internal readonly struct InheritedSettings
    {
        private readonly byte classBehavior;

        private readonly byte propertyBehavior;

        public InheritedSettings(
            IValidationMessageProvider? messageProvider,
            ValidationBehavior? classBehavior,
            ValidationBehavior? propertyBehavior,
            ValidationGroups? groups)
        {
            this.MessageProvider = messageProvider;
            this.classBehavior = Pack(classBehavior);
            this.propertyBehavior = Pack(propertyBehavior);
            this.Groups = groups;
        }

        public IValidationMessageProvider? MessageProvider { get; }

        public ValidationGroups? Groups { get; }

        public ValidationBehavior? Class => Unpack(this.classBehavior);

        public ValidationBehavior? Property => Unpack(this.propertyBehavior);

        public static InheritedSettings From(NValidationOptions? options)
        {
            return options is null
                ? default
                : new InheritedSettings(
                    options.MessageProvider,
                    options.ValidationBehaviors.Class,
                    options.ValidationBehaviors.Property,
                    options.ValidationGroups);
        }

        public NValidationOptions? AsOptions()
        {
            if (this.MessageProvider is null && this.classBehavior == 0 && this.propertyBehavior == 0 && this.Groups is null)
            {
                return null;
            }

            return new NValidationOptions
            {
                MessageProvider = this.MessageProvider,
                ValidationBehaviors = new ValidationBehaviors { Class = this.Class, Property = this.Property },
                ValidationGroups = this.Groups,
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
