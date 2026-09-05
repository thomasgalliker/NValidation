namespace NValidation.AspNetCore
{
    /// <summary>
    /// What <see cref="ValidationActionFilter"/> does with a payload it cannot validate: one which has
    /// neither a registered <see cref="IValidator{T}"/> nor a <see cref="SkipValidationAttribute"/>.
    /// </summary>
    public enum MissingValidatorBehavior
    {
        /// <summary>
        /// Let the action run. The default, so adding the filter to an application whose payloads have not
        /// been gone through yet changes nothing.
        /// </summary>
        Ignore = 0,

        /// <summary>
        /// Let the action run, and log one warning naming the action and the parameter type.
        /// </summary>
        Log = 1,

        /// <summary>
        /// Throw an <see cref="InvalidOperationException"/>. Turns a payload nobody validates into a loud
        /// failure while it can still be fixed; intended for development and test hosts, not production.
        /// </summary>
        Throw = 2,
    }
}
