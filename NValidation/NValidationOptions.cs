namespace NValidation
{
    /// <summary>
    /// The three settings a validator can be given without a container: where its messages come from,
    /// how much it reports, and which rule groups it runs. Immutable; a variant is derived with
    /// <c>with</c>. A setting left <c>null</c> means "the level below answers".
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
    /// The group selection has no validator level: which rules apply is a fact about the call rather
    /// than about the validator, so it is named by the options of the call, the registration or
    /// <see cref="Default"/>, and failing all of those only the chains in no group run.
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
        /// below; failing every level, <see cref="ValidationGroups.None"/>, so only the chains in no
        /// group run. Converted from what is written: <c>ValidationGroups = "Create"</c>.
        /// </summary>
        public ValidationGroups? ValidationGroups { get; init; }
    }
}
