using System.Linq.Expressions;

namespace NValidation.Tests.TestData
{
    /// <summary>
    /// A validator whose rules are declared by the test rather than in a constructor:
    /// <code>
    /// var validator = new TestValidator&lt;Car&gt;();
    /// validator.Property(c => c.Vin).NotEmpty().MaximumLength(17);
    /// </code>
    /// So a test about one rule states that rule where it asserts about it, instead of naming a class
    /// the reader has to go and open.
    /// </summary>
    /// <remarks>
    /// Not a different kind of validator: <see cref="Property{TProperty}"/> is
    /// <see cref="Validator{T}.Property{TProperty}"/> made reachable and nothing else, so a rule
    /// declared here behaves exactly as the same rule declared in a validator an application ships.
    /// That is the point — what the test proves is what a caller gets.
    /// <para>
    /// Declare every rule before validating. The rules are walked as a plain list, so one appended
    /// after a run does take part in the next one — but the display names are resolved on the first run
    /// and kept, so a <c>WithDisplayName</c> written afterwards never reaches a message: it falls back
    /// to the property's code. Arrange-then-Act never does that, which is why this stays in the test
    /// project rather than beside <see cref="Validator{T}"/>.
    /// </para>
    /// </remarks>
    internal sealed class TestValidator<T> : Validator<T>
    {
        /// <inheritdoc cref="Validator{T}.Property{TProperty}"/>
        public new PropertyRuleBuilder<T, TProperty?> Property<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            return base.Property(expression);
        }
    }
}
