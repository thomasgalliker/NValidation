using System.Linq.Expressions;

namespace NValidation.Testing
{
    /// <summary>
    /// A validator whose rules are declared by the test rather than in a constructor:
    /// <code>
    /// var validator = new TestValidator&lt;Invoice&gt;();
    /// validator.Property(i => i.Reference).NotEmpty().MaximumLength(32);
    /// </code>
    /// So a test about one rule states that rule where it asserts about it, instead of naming a class the
    /// reader has to go and open.
    /// </summary>
    /// <remarks>
    /// Not a different kind of validator: <see cref="Property{TProperty}"/> is
    /// <see cref="Validator{T}.Property{TProperty}(System.Linq.Expressions.Expression{System.Func{T, TProperty}})"/> made reachable and nothing else, so a rule declared
    /// here behaves exactly as the same rule declared in a validator an application ships. That is the
    /// point — what the test proves is what a caller gets.
    /// <para>
    /// Declare every rule before validating. The rules are walked as a plain list, so one appended after
    /// a run does take part in the next one — but the display names are resolved on the first run and
    /// kept, so a <c>WithDisplayName</c> written afterwards never reaches a message: it falls back to the
    /// property's name. Arrange-then-Act never does that.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type being validated.</typeparam>
    public sealed class TestValidator<T> : Validator<T>
    {
        /// <summary>
        /// A validator answering through whatever nothing more specific has overruled — the built-in
        /// English, unless a test configured <see cref="NValidationOptions.Default"/> — for a test about
        /// the message a rule renders.
        /// </summary>
        public TestValidator()
        {
        }

        /// <summary>
        /// A validator answering through <paramref name="messages"/>. Pass
        /// <see cref="ErrorCodeProvider.Instance"/> for a test about <em>which</em> rule reported, which
        /// then does not depend on any wording.
        /// </summary>
        public TestValidator(IValidationMessageProvider messages)
        {
            this.Messages = messages;
        }

        /// <inheritdoc cref="Validator{T}.Property{TProperty}(System.Linq.Expressions.Expression{System.Func{T, TProperty}})"/>
        public new PropertyRuleBuilder<T, TProperty?> Property<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            return base.Property(expression);
        }
    }
}
