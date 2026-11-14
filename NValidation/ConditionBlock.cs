namespace NValidation
{
    /// <summary>
    /// A condition block just declared with <c>When</c> or <c>Unless</c>, to which <see cref="Otherwise"/>
    /// adds the chains for the opposite case.
    /// </summary>
    public readonly struct ConditionBlock<T>
    {
        private readonly Validator<T>? validator;

        private readonly Func<T, bool>? otherwise;

        internal ConditionBlock(Validator<T> validator, Func<T, bool> otherwise)
        {
            this.validator = validator;
            this.otherwise = otherwise;
        }

        /// <summary>
        /// Declares the chains that apply where the block's condition does not hold:
        /// <code>this.When(m => m.EngineType == EngineType.Electric, () => ...).Otherwise(() => ...);</code>
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="declareRules"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">
        /// This block was not returned by <c>When</c> or <c>Unless</c>, or the validator has already validated
        /// something.
        /// </exception>
        public void Otherwise(Action declareRules)
        {
            ArgumentNullException.ThrowIfNull(declareRules);

            if (this.validator is null || this.otherwise is null)
            {
                throw new InvalidOperationException(
                    "Otherwise follows the block a When or Unless returned, e.g. this.When(..., () => ...).Otherwise(() => ...).");
            }

            this.validator.DeclareWhen(this.otherwise, declareRules);
        }
    }
}
