namespace NValidation
{
    /// <summary>
    /// A covariant view of a rule chain, for the rules which have to know the shape of the property
    /// without being able to name its exact type.
    /// </summary>
    /// <remarks>
    /// <typeparamref name="TProperty"/> appears in no member, which is what lets this be covariant while
    /// <see cref="PropertyRuleBuilder{T, TProperty}"/> — whose rules consume the property's value — cannot
    /// be. The covariance is the point: it is what lets a chain for a <c>List&lt;TElement&gt;</c> be seen as
    /// a chain for an <c>IEnumerable&lt;TElement&gt;</c>, so a rule declared for elements can infer
    /// <c>TElement</c> from the chain it is written on. The instance being validated does not appear here
    /// either: a rule reached through this view answers about the property alone.
    /// <para>
    /// Infrastructure, not an extension point: the member is internal, so nothing outside this library
    /// implements it.
    /// </para>
    /// </remarks>
    public interface IPropertyRuleTarget<out TProperty>
    {
        internal void AddElementRule<TElement>(ElementRuleBuilder<TElement> elements);
    }
}
