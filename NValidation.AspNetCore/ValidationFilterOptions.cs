namespace NValidation.AspNetCore
{
    /// <summary>
    /// Configures <see cref="ValidationActionFilter"/>.
    /// </summary>
    public sealed class ValidationFilterOptions
    {
        /// <summary>
        /// What to do with a body- or form-bound parameter that has neither a registered
        /// <see cref="IValidator{T}"/> nor a <see cref="SkipValidationAttribute"/>. Defaults to
        /// <see cref="MissingValidatorBehavior.Ignore"/>.
        /// </summary>
        public MissingValidatorBehavior MissingValidatorBehavior { get; set; } = MissingValidatorBehavior.Ignore;
    }
}
