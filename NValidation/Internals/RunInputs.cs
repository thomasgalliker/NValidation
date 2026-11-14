namespace NValidation.Internals
{
    /// <summary>
    /// What a call hands every pass of its run: the group selection, the data the rules may read, and the
    /// properties it is limited to. One reference — the selection itself where the call brought nothing
    /// else, a <see cref="CallInputs"/> where it did — so carrying the rest costs a frame nothing, and a call
    /// without it allocates nothing to say so.
    /// </summary>
    internal readonly struct RunInputs
    {
        private readonly object? value;

        private RunInputs(object value)
        {
            this.value = value;
        }

        public ValidationGroups Groups => this.value as ValidationGroups
            ?? (this.value is CallInputs inputs ? inputs.Groups : ValidationGroups.None);

        public ValidationData? Data => (this.value as CallInputs)?.Data;

        public ValidationProperties? Properties => (this.value as CallInputs)?.Properties;

        public bool IsSameAs(RunInputs other)
        {
            return ReferenceEquals(this.value, other.value);
        }

        /// <summary>
        /// The selection, and the properties the call is limited to, for a pass which needs both: one type
        /// test rather than one per question.
        /// </summary>
        public ValidationGroups Unpack(out ValidationProperties? properties)
        {
            if (this.value is ValidationGroups groups)
            {
                properties = null;

                return groups;
            }

            if (this.value is CallInputs inputs)
            {
                properties = inputs.Properties;

                return inputs.Groups;
            }

            properties = null;

            return ValidationGroups.None;
        }

        /// <summary>
        /// The same inputs with the additive form of the selection, for a validator that takes no part in an
        /// exclusive one. Built once per selection or pair, on first use.
        /// </summary>
        public RunInputs Additive => this.value is CallInputs inputs
            ? new RunInputs(inputs.Additive)
            : new RunInputs(this.Groups.Additive);

        public static RunInputs Of(ValidationGroups groups, ValidationData? data, ValidationProperties? properties)
        {
            if (data is { Count: 0 })
            {
                data = null;
            }

            if (properties != null)
            {
                return new RunInputs(properties.InputsFor(groups, data));
            }

            return data is null
                ? new RunInputs(groups)
                : new RunInputs(data.InputsFor(groups));
        }

        /// <summary>
        /// The same inputs limited to <paramref name="properties"/> instead, for what a chain hands its value
        /// to: the part of the selection below the chain, or null for everything.
        /// </summary>
        public RunInputs WithProperties(ValidationProperties? properties)
        {
            return Of(this.Groups, this.Data, properties);
        }
    }

    internal sealed class CallInputs
    {
        private CallInputs? additive;

        public CallInputs(ValidationGroups groups, ValidationData? data, ValidationProperties? properties)
        {
            this.Groups = groups;
            this.Data = data;
            this.Properties = properties;
        }

        public ValidationGroups Groups { get; }

        public ValidationData? Data { get; }

        public ValidationProperties? Properties { get; }

        public CallInputs Additive => this.Groups.IncludesDefault
            ? this
            : this.additive ??= new CallInputs(this.Groups.Additive, this.Data, this.Properties);
    }

    /// <summary>
    /// What a pass does with one chain: runs it, runs only the part that hands its value to another
    /// validator, or passes it by.
    /// </summary>
    internal enum ChainGate
    {
        Skip,
        Run,
        Composed,
    }
}
