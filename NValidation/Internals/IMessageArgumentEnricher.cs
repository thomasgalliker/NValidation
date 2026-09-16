namespace NValidation.Internals
{
    /// <summary>
    /// A message provider which also knows something the rule did not: the element of a collection the
    /// failure is about.
    /// </summary>
    /// <remarks>
    /// A rule's arguments normally reach the provider and are completed there. A message the chain
    /// supplied through <c>WithMessage</c> never reaches it — it is a template the chain owns, formatted
    /// directly — so a rule inside a <c>ForEach</c> asks through this instead, and the two spellings of
    /// a message name the same things.
    /// </remarks>
    internal interface IMessageArgumentEnricher
    {
        IReadOnlyDictionary<string, object?> Enrich(IReadOnlyDictionary<string, object?> arguments);
    }
}
