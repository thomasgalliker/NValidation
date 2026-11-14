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
    /// Named for the library rather than for the act, because .NET 10's shared framework ships
    /// <c>Microsoft.Extensions.Validation.SkipValidationAttribute</c>. A consumer importing both
    /// namespaces could not write the shorter name at all: that is a CS0104 ambiguity, not a warning.
    /// </remarks>
    /// <example>
    /// <code>
    /// public Task&lt;IActionResult&gt; ImportAsync(
    ///     [SkipNValidation("Reports failures per row, not as a 400.")] CarImportDto carImportDto)
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Parameter)]
    public sealed class SkipNValidationAttribute : Attribute
    {
        /// <summary>
        /// Creates the attribute without stating a reason.
        /// </summary>
        public SkipNValidationAttribute()
        {
        }

        /// <summary>
        /// Creates the attribute, stating in <paramref name="reason"/> why this payload is not
        /// validated by the filter.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="reason"/> is null, empty or whitespace.</exception>
        public SkipNValidationAttribute(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException(
                    "A stated reason must say something. Write [SkipNValidation] to skip without one.", nameof(reason));
            }

            this.Reason = reason;
        }

        /// <summary>
        /// What the constructor was told, or <c>null</c> where no reason was stated.
        /// </summary>
        public string? Reason { get; }
    }
}
