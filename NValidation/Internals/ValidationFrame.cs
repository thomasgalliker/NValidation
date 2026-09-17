namespace NValidation.Internals
{
    internal abstract class ValidationFrame
    {
        private List<ValidationError>? errors;

        /// <summary>
        /// errors: The list to report into where the caller owns one — an entry of a ForEach, or a validator
        /// this one composed — and null for a pass which reports into a list of its own, which is then built
        /// only if something actually fails.
        ///
        /// run: The run this pass belongs to.
        /// </summary>
        protected ValidationFrame(List<ValidationError>? errors, ValidationRun run)
        {
            this.errors = errors;
            this.Run = run;
        }

        /// <summary>
        /// Where failures go, built on first use. A rule which only counts what has been reported asks
        /// ErrorCount instead, so a pass that finds nothing never builds a list.
        /// </summary>
        public List<ValidationError> Errors => this.errors ??= [];

        /// <summary>
        /// How many failures have been reported, without building Errors.
        /// </summary>
        public int ErrorCount => this.errors?.Count ?? 0;

        public ValidationRun Run { get; }

        /// <summary>
        /// What this pass reported, where it owned the list and there is something in it. The list is handed
        /// over rather than copied: the pass is finished with it by the time this is asked.
        /// </summary>
        public bool TryTakeErrors(out List<ValidationError> reported)
        {
            reported = this.errors!;

            return this.errors is { Count: > 0 };
        }
    }

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
