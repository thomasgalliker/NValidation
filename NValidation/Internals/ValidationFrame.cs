namespace NValidation.Internals
{
    /// <summary>
    /// What every chain of one validator's pass over one object shares: the list its failures go into
    /// and the <see cref="Internals.ValidationRun"/> the pass belongs to. The generic part below adds
    /// the object itself.
    /// </summary>
    /// <remarks>
    /// One object per pass rather than one per chain, so <see cref="RuleContext{T, TProperty}"/> can be
    /// a struct over it and a chain costs no allocation. A pass, not a run: a <c>ForEach</c> validates
    /// each entry as its own pass, and a composed validator reports into the list its caller owns.
    /// <para>
    /// The non-generic part is what a composed rule hands to an <see cref="ElementRuleBuilder{TElement}"/>,
    /// which does not know the composer's type: passing the frame is free, whereas passing a method
    /// group of the struct context boxed the context and allocated a delegate on every validation.
    /// </para>
    /// </remarks>
    internal abstract class ValidationFrame
    {
        private List<ValidationError>? errors;

        /// <param name="errors">
        /// The list to report into where the caller owns one — an entry of a <c>ForEach</c>, or a
        /// validator this one composed — and <c>null</c> for a pass which reports into a list of its
        /// own, which is then built only if something actually fails.
        /// </param>
        /// <param name="run">The run this pass belongs to.</param>
        protected ValidationFrame(List<ValidationError>? errors, ValidationRun run)
        {
            this.errors = errors;
            this.Run = run;
        }

        /// <summary>
        /// Where failures go, built on first use. A rule which only counts what has been reported asks
        /// <see cref="ErrorCount"/> instead, so a pass that finds nothing never builds a list.
        /// </summary>
        public List<ValidationError> Errors => this.errors ??= [];

        /// <inheritdoc cref="Errors"/>
        public int ErrorCount => this.errors?.Count ?? 0;

        public ValidationRun Run { get; }

        /// <summary>
        /// What this pass reported, where it owned the list and there is something in it. The list is
        /// handed over rather than copied: the pass is finished with it by the time this is asked.
        /// </summary>
        public bool TryTakeErrors(out List<ValidationError> reported)
        {
            reported = this.errors!;

            return this.errors is { Count: > 0 };
        }
    }

    /// <inheritdoc cref="ValidationFrame"/>
    internal sealed class ValidationFrame<T> : ValidationFrame
    {
        public ValidationFrame(T instance, List<ValidationError>? errors, ValidationRun run)
            : base(errors, run)
        {
            this.Instance = instance;
        }

        public T Instance { get; }
    }
}
