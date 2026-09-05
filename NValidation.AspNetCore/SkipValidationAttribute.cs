namespace NValidation.AspNetCore
{
    /// <summary>
    /// Excludes a parameter, an action or a controller from <see cref="ValidationActionFilter"/>, for the
    /// cases where a validator exists but must not run automatically — an endpoint which reports failures
    /// in its own shape, or one whose payload is validated further in.
    /// </summary>
    /// <remarks>
    /// The exclusion also silences <see cref="ValidationFilterOptions.MissingValidatorBehavior"/>: what is
    /// marked here is a decision, not an oversight. State a <see cref="Reason"/> wherever the next reader
    /// would otherwise have to reconstruct which decision it was; leave it out where the action says so
    /// itself.
    /// </remarks>
    /// <example>
    /// <code>
    /// public Task&lt;IActionResult&gt; ImportAsync(
    ///     [SkipValidation("Reports failures per row, not as a 400.")] CarImportDto carImportDto)
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Parameter)]
    public sealed class SkipValidationAttribute : Attribute
    {
        /// <summary>
        /// Creates the attribute without stating a reason.
        /// </summary>
        public SkipValidationAttribute()
        {
        }

        /// <summary>
        /// Creates the attribute, stating why this payload is not validated by the filter.
        /// </summary>
        /// <param name="reason">Why this payload is not validated by the filter.</param>
        /// <exception cref="ArgumentException"><paramref name="reason"/> is null, empty or whitespace.</exception>
        public SkipValidationAttribute(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException(
                    "A stated reason must say something. Write [SkipValidation] to skip without one.", nameof(reason));
            }

            this.Reason = reason;
        }

        /// <summary>
        /// Why this payload is not validated by the filter, or <c>null</c> where none was stated.
        /// </summary>
        public string? Reason { get; }
    }
}
