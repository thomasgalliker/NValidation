namespace NValidation
{
    /// <summary>
    /// The settings a validator can be given without a container: where its messages come from, how much
    /// it reports, which rule groups it runs, which data its rules may read, and which properties it is
    /// limited to. Immutable; a variant is derived with <c>with</c>. A setting left <c>null</c> means "the
    /// level below answers".
    /// </summary>
    /// <example>
    /// <code>
    /// NValidationOptions.Default = new NValidationOptions
    /// {
    ///     MessageProvider = new ResourceValidationMessageProvider(),
    ///     ValidationBehaviors = new() { Property = ValidationBehavior.All },
    /// };
    /// </code>
    /// </example>
    /// <remarks>
    /// Resolved from the most specific level that names a setting: what the validator declared for
    /// itself; then what it inherits — the options passed to the call, or, for a validator composed into
    /// another, what its composer resolved; then what <c>AddNValidation</c> configured, for a validator
    /// the container built; then <see cref="Default"/>; then the built-in English and the built-in
    /// behaviors. Everything a validator resolves is inherited by the validators it composes and by the
    /// element chain of a <c>ForEach</c>, unless they declared otherwise for themselves.
    /// The group selection, the data and the properties have no validator level: which rules apply, and
    /// what they may read about the request, are facts about the call rather than about the validator, so
    /// they are named by the options of the call, the registration or <see cref="Default"/>; failing all of
    /// those only the default group runs, over every property, and no data is handed over.
    /// <see cref="Default"/> is for an application to set, not a library: a package which assigns it
    /// changes the wording every one of its consumers sees.
    /// </remarks>
    public sealed record NValidationOptions
    {
        /// <summary>
        /// Volatile: read on whatever thread a validation runs on, and a host which assigns at startup has to
        /// be sure every later run sees it.
        /// </summary>
        private static volatile NValidationOptions current = new();

        internal static NValidationOptions None { get; } = new();

        /// <summary>
        /// The options every validator in the process falls back to. Assign once at startup, before
        /// anything validates.
        /// </summary>
        /// <remarks>
        /// Assigning replaces a reference rather than changing an object, so it is safe at any time: a
        /// run already reading the old options keeps them, and later runs read the new ones.
        /// </remarks>
        /// <exception cref="ArgumentNullException">The value is <c>null</c>.</exception>
        public static NValidationOptions Default
        {
            get => current;
            set => current = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Where the rules take their message texts from, or <c>null</c> — the default — to leave it to
        /// the level below. A provider has to be thread-safe: validators run concurrently, and one
        /// provider serves them all.
        /// </summary>
        public IValidationMessageProvider? MessageProvider { get; init; }

        /// <summary>
        /// How much a validator reports. Both axes start at <c>null</c>, meaning the level below answers.
        /// </summary>
        public ValidationBehaviors ValidationBehaviors { get; init; }

        /// <summary>
        /// Which rule groups a validation runs, or <c>null</c> — the default — to leave it to the level
        /// below; failing every level, <see cref="ValidationGroups.None"/>, so only the default group runs.
        /// Converted from what is written: <c>ValidationGroups = "Create"</c>.
        /// </summary>
        /// <remarks>
        /// Resolved once, where the call starts, and handed to every validator the call reaches. An
        /// exclusive selection — <see cref="NValidation.ValidationGroups.Only"/> — set on <see cref="Default"/>
        /// applies to every call that names no groups of its own, and the validator of each such call has to
        /// declare one of its groups.
        /// </remarks>
        public ValidationGroups? ValidationGroups { get; init; }

        /// <summary>
        /// Values the caller hands the rules — facts about the request the object cannot answer, such as the
        /// market a car is offered in — or <c>null</c>, the default, to leave it to the level below:
        /// <c>ValidationData = [new ListingPolicy(MaximumMileage: 200_000, RequiresServiceHistory: true)]</c>.
        /// </summary>
        /// <remarks>
        /// A rule asks for a value by its type, and a chain asking for a type the call did not hand over is
        /// skipped. The most specific level that names data supplies all of it: the levels are not merged.
        /// Resolved once, where the call starts, and handed to every validator the call reaches.
        /// </remarks>
        public ValidationData? ValidationData { get; init; }

        /// <summary>
        /// The properties a validation is limited to, named as failures are reported, or <c>null</c> — the
        /// default — to leave it to the level below; failing every level, every property:
        /// <c>ValidationProperties = ["PurchasePrice", "Model.Name"]</c>.
        /// </summary>
        /// <remarks>
        /// Applies on top of <see cref="ValidationGroups"/>: a chain runs only where both select it.
        /// Resolved once, where the call starts, and narrowed on the way into every validator the call
        /// reaches.
        /// </remarks>
        public ValidationProperties? ValidationProperties { get; init; }
    }
}
