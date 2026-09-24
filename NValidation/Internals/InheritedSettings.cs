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
            RunInputs inputs)
        {
            this.MessageProvider = messageProvider;
            this.classBehavior = Pack(classBehavior);
            this.propertyBehavior = Pack(propertyBehavior);
            this.Inputs = inputs;
        }

        public IValidationMessageProvider? MessageProvider { get; }

        public RunInputs Inputs { get; }

        public ValidationGroups Groups => this.Inputs.Groups;

        public ValidationBehavior? Class => Unpack(this.classBehavior);

        public ValidationBehavior? Property => Unpack(this.propertyBehavior);

        /// <summary>
        /// The run as options, for a validator written by hand. It declares nothing the library can see, so
        /// it is handed the additive form of the selection: it cannot take part in an exclusive one.
        /// </summary>
        public NValidationOptions AsOptions()
        {
            return new NValidationOptions
            {
                MessageProvider = this.MessageProvider,
                ValidationBehaviors = new ValidationBehaviors { Class = this.Class, Property = this.Property },
                ValidationGroups = this.Inputs.Groups.Additive,
                ValidationData = this.Inputs.Data,
                ValidationProperties = this.Inputs.Properties,
            };
        }

        /// <summary>
        /// Whether these are the settings <paramref name="other"/> holds, instance for instance, which is what
        /// lets a pass reuse a resolution kept for them.
        /// </summary>
        public bool IsSameAs(in InheritedSettings other)
        {
            return ReferenceEquals(this.MessageProvider, other.MessageProvider)
                && this.classBehavior == other.classBehavior
                && this.propertyBehavior == other.propertyBehavior
                && this.Inputs.IsSameAs(other.Inputs);
        }

        public InheritedSettings WithInputs(RunInputs inputs)
        {
            return new InheritedSettings(this.MessageProvider, this.Class, this.Property, inputs);
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
