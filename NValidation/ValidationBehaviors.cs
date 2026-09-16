namespace NValidation
{
    /// <summary>
    /// How much a validator reports, along the two axes it can be asked about: across its properties,
    /// and within one property's rule chain. Carried by <see cref="Validator{T}.ValidationBehaviors"/>
    /// and by <c>NValidationBuilder.ValidationBehaviors</c>, so the level a setting applies to is
    /// where it is written rather than a word in its name.
    /// </summary>
    /// <remarks>
    /// Both axes are <c>null</c> until something sets them, and <c>null</c> means "inherit from the
    /// level above" — a validator that names one axis leaves the other taking whatever the registration
    /// configured, and a registration that names neither leaves both at the built-in defaults. That is
    /// why this is mutated rather than assigned: replacing the whole object would also replace the axis
    /// the caller did not mean to touch.
    /// </remarks>
    public sealed class ValidationBehaviors
    {
        private ValidationBehavior? classBehavior;

        private ValidationBehavior? propertyBehavior;

        /// <summary>
        /// Whether these axes can still be changed. Set when the <see cref="NValidationOptions"/> they
        /// belong to is first used, and never for the ones a validator or a registration holds.
        /// </summary>
        internal bool IsReadOnly { get; private set; }

        /// <summary>
        /// Whether a run keeps going once a property has reported. Defaults to
        /// <see cref="ValidationBehavior.All"/>, so a caller is told about every field that is wrong
        /// rather than being sent back one field at a time.
        /// </summary>
        /// <remarks>
        /// <see cref="ValidationBehavior.StopAtFirstError"/> stops the run as soon as anything has been
        /// reported: the properties after it are never looked at. It therefore also stops the chain
        /// that produced it, overriding <see cref="Property"/> — a setting by that name which went on
        /// judging the same property would be a trap. A chain which says otherwise through
        /// <c>WithValidationBehavior</c> is the one exception, because it says so at the point it
        /// applies.
        /// <para>
        /// That usually means one message, but not always: a run is stopped between rules, and a rule
        /// which reported several at once is not cut short. A validator merged in with
        /// <c>SetValidator</c> has already had its own say, and a <c>ForEach</c> reports on every entry
        /// it walked. What a composed validator found is its decision rather than its composer's, so it
        /// is passed on whole.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// These axes belong to an <see cref="NValidationOptions"/> which has already been used.
        /// </exception>
        public ValidationBehavior? Class
        {
            get => this.classBehavior;

            set
            {
                this.ThrowIfReadOnly();

                this.classBehavior = value;
            }
        }

        /// <summary>
        /// Whether a property's chain keeps going once one of its rules has failed. Defaults to
        /// <see cref="ValidationBehavior.StopAtFirstError"/>, so a property reports at most one message.
        /// </summary>
        /// <remarks>
        /// Stopping is the better default because the rules of a chain are usually ordered from coarse
        /// to fine: an empty string fails <c>NotEmpty</c> and <c>Length(17)</c> alike, and only the
        /// first of those tells the caller anything. <see cref="ValidationBehavior.All"/> is for the
        /// chain whose rules are genuinely independent — a password that is judged on length, digits and
        /// case at once.
        /// </remarks>
        /// <inheritdoc cref="Class" path="/exception"/>
        public ValidationBehavior? Property
        {
            get => this.propertyBehavior;

            set
            {
                this.ThrowIfReadOnly();

                this.propertyBehavior = value;
            }
        }

        /// <summary>
        /// Refuses every later change to these axes.
        /// </summary>
        internal void MakeReadOnly()
        {
            this.IsReadOnly = true;
        }

        /// <summary>
        /// Puts both axes back to inheriting, and allows changes again.
        /// </summary>
        internal void Reset()
        {
            this.IsReadOnly = false;
            this.classBehavior = null;
            this.propertyBehavior = null;
        }

        private void ThrowIfReadOnly()
        {
            if (this.IsReadOnly)
            {
                throw new InvalidOperationException(
                    "These validation behaviors have already been used, so changing them now would " +
                    "apply unevenly: what has already validated used the old ones. Configure them " +
                    $"before anything validates, or call {nameof(NValidationOptions)}.{nameof(NValidationOptions.Reset)}() first.");
            }
        }
    }
}
