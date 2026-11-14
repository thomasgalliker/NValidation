namespace NValidation
{
    /// <summary>
    /// A covariant view of a rule chain, for the rules which have to know the shape of the property
    /// without being able to name its exact type.
    /// </summary>
    /// <remarks>
    /// Covariant in <typeparamref name="TProperty"/>, which is what lets a chain for a
    /// <c>List&lt;TElement&gt;</c> be seen as a chain for an <c>IEnumerable&lt;TElement&gt;</c>, so a rule
    /// declared for elements can infer <c>TElement</c> from the chain it is written on. Infrastructure,
    /// not an extension point: its only member is internal.
    /// </remarks>
    public interface IPropertyRuleTarget<out TProperty>
    {
        internal void AddElementRule<TElement>(ElementRuleBuilder<TElement> elements);
    }
}
