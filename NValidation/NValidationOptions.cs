namespace NValidation
{
    /// <summary>
    /// What a validator falls back to when nothing more specific said: the provider its rules take their
    /// message texts from, and how much it reports. Set <see cref="Default"/> once at startup and every
    /// validator in the process picks it up, so a validator constructed with <c>new</c> does not have to
    /// be configured one instance at a time.
    /// </summary>
    /// <example>
    /// <code>
    /// NValidationOptions.Default.MessageProvider = new ResourceValidationMessageProvider();
    /// NValidationOptions.Default.ValidationBehaviors.Property = ValidationBehavior.All;
    /// </code>
    /// </example>
    /// <remarks>
    /// Resolved from the most specific level that names a setting:
    /// <list type="number">
    /// <item><description>what the validator was handed itself — <see cref="Validator{T}.Messages"/>,
    /// <see cref="Validator{T}.ValidationBehaviors"/>;</description></item>
    /// <item><description>what <c>AddNValidation</c> configured for it;</description></item>
    /// <item><description><see cref="Default"/>;</description></item>
    /// <item><description>the built-in English, every property, one message each.</description></item>
    /// </list>
    /// Unlike a setting on <c>AddNValidation</c>, which is handed to the validators the container built,
    /// these are read while validating — so they also reach a nested validator and the element chain of
    /// a <c>ForEach</c>, neither of which the container ever constructed.
    /// <para>
    /// <b>For an application to set, not a library.</b> A package which configures <see cref="Default"/>
    /// from a module initializer silently changes the wording every one of its consumers sees.
    /// </para>
    /// <para>
    /// <b>Unlike <c>JsonSerializerOptions.Default</c></b>, which is read-only from the start, this is
    /// mutable until something validates. From that point it is frozen — <see cref="IsReadOnly"/> is
    /// <c>true</c> and every setter throws — so nothing a run is reading can change underneath it.
    /// <see cref="Reset"/> is the way back.
    /// </para>
    /// <para>
    /// A <see cref="MessageProvider"/> whose type comes from a collectible <c>AssemblyLoadContext</c>
    /// belongs on that context's own container rather than here: <see cref="Default"/> holds the
    /// instance for the life of the process, and would root the context with it.
    /// </para>
    /// </remarks>
    public sealed class NValidationOptions
    {
        private IValidationMessageProvider messageProvider = DefaultValidationMessageProvider.Instance;

        /// <summary>
        /// Options at the built-in defaults.
        /// </summary>
        public NValidationOptions()
        {
        }

        /// <summary>
        /// Options starting from a copy of <paramref name="from"/>, which the copy does not go on
        /// sharing: the new options are unfrozen and changing either leaves the other alone.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="from"/> is <c>null</c>.</exception>
        public NValidationOptions(NValidationOptions from)
        {
            ArgumentNullException.ThrowIfNull(from);

            this.messageProvider = from.messageProvider;
            this.ValidationBehaviors.Class = from.ValidationBehaviors.Class;
            this.ValidationBehaviors.Property = from.ValidationBehaviors.Property;
        }

        /// <summary>
        /// The options every validator in the process falls back to. Mutated rather than assigned, so
        /// naming one setting leaves the others alone and nothing can replace the object a run is
        /// already reading.
        /// </summary>
        public static NValidationOptions Default { get; } = new();

        /// <summary>
        /// Where the rules take their message texts from. The built-in English until something says
        /// otherwise.
        /// </summary>
        /// <remarks>
        /// A provider has to be thread-safe, because validators run concurrently and one provider serves
        /// them all. Resolve the language while the message is produced — a <see cref="Func{TResult}"/>
        /// over a resource — rather than in the constructor.
        /// </remarks>
        /// <exception cref="ArgumentNullException">The value is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">These options have already been used.</exception>
        public IValidationMessageProvider MessageProvider
        {
            get => this.messageProvider;

            set
            {
                ArgumentNullException.ThrowIfNull(value);
                this.ThrowIfReadOnly();

                this.messageProvider = value;
            }
        }

        /// <summary>
        /// How much a validator reports: across its properties, and within one property's chain. Mutated
        /// rather than assigned — <c>NValidationOptions.Default.ValidationBehaviors.Class = ValidationBehavior.StopAtFirstError;</c>
        /// — so naming one axis leaves the other inheriting.
        /// </summary>
        public ValidationBehaviors ValidationBehaviors { get; } = new();

        /// <summary>
        /// Whether these options have been used, and can therefore no longer change.
        /// </summary>
        public bool IsReadOnly { get; private set; }

        /// <summary>
        /// Refuses every later change to these options, without waiting for the first validation to do
        /// it. For a host which wants a misplaced configuration call to fail at startup rather than
        /// whenever validation first happens to run.
        /// </summary>
        public void MakeReadOnly()
        {
            this.IsReadOnly = true;
            this.ValidationBehaviors.MakeReadOnly();
        }

        /// <summary>
        /// Puts every setting back to the built-in defaults and allows changes again: the built-in
        /// English, every property, one message each.
        /// </summary>
        /// <remarks>
        /// The way back from <see cref="IsReadOnly"/>, which is what makes these options usable from a
        /// test suite, a benchmark's iteration setup, or a host undoing its own configuration. There is
        /// no equivalent on <c>JsonSerializerOptions</c>, which is never un-frozen.
        /// </remarks>
        public void Reset()
        {
            this.IsReadOnly = false;
            this.messageProvider = DefaultValidationMessageProvider.Instance;
            this.ValidationBehaviors.Reset();
        }

        /// <summary>
        /// <see cref="Default"/>, marked as used so it can no longer change under a run. Reading
        /// <see cref="Default"/> itself does not freeze it, so a caller may still inspect it.
        /// </summary>
        internal static NValidationOptions DefaultInUse()
        {
            var options = Default;

            if (!options.IsReadOnly)
            {
                options.MakeReadOnly();
            }

            return options;
        }

        private void ThrowIfReadOnly()
        {
            if (this.IsReadOnly)
            {
                throw new InvalidOperationException(
                    "These validation options have already been used, so changing them now would apply " +
                    "unevenly: what has already validated used the old ones. Configure them before " +
                    $"anything validates, or call {nameof(Reset)}() first.");
            }
        }
    }
}
