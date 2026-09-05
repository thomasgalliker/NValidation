using NValidation.Internals;

namespace NValidation
{
    /// <summary>
    /// The chainable part of <see cref="Validator{T}.Property{TProperty}"/>. Rules are extension methods
    /// on this type, so an application can add its own without touching the core.
    /// </summary>
    public readonly struct PropertyRuleBuilder<T, TProperty> : IPropertyRuleTarget<TProperty>
    {
        private readonly PropertyRule<T, TProperty> rule;

        internal PropertyRuleBuilder(PropertyRule<T, TProperty> rule)
        {
            this.rule = rule;
        }

        /// <summary>
        /// Appends a rule to this property's chain.
        /// </summary>
        public PropertyRuleBuilder<T, TProperty> Add(Action<RuleContext<T, TProperty>> check)
        {
            ArgumentNullException.ThrowIfNull(check);

            this.RequireRule().Add((context, _) =>
            {
                check(context);
                return ValueTask.CompletedTask;
            });

            return this;
        }

        /// <summary>
        /// Appends a rule which needs to await something, e.g. a nested validator.
        /// </summary>
        public PropertyRuleBuilder<T, TProperty> AddAsync(Func<RuleContext<T, TProperty>, CancellationToken, ValueTask> check)
        {
            ArgumentNullException.ThrowIfNull(check);

            this.RequireRule().Add(check);

            return this;
        }

        /// <summary>
        /// Reports every violated rule of this property instead of stopping at the first one.
        /// </summary>
        public PropertyRuleBuilder<T, TProperty> ContinueOnFailure()
        {
            this.RequireRule().ContinueOnFailure = true;

            return this;
        }

        /// <summary>
        /// Replaces the message of the rule just written, for the cases where the shared wording does
        /// not fit: <c>this.Property(x => x.Name).NotEmpty().WithMessage("Please tell us your name.");</c>
        /// </summary>
        /// <remarks>
        /// Applies to that one rule, not to the whole chain, so each rule of a property can carry its own
        /// wording. The error code is unaffected.
        /// </remarks>
        public PropertyRuleBuilder<T, TProperty> WithMessage(string message)
        {
            ArgumentNullException.ThrowIfNull(message);

            return this.WithMessage(() => message);
        }

        /// <summary>
        /// The same, for a message which has to be resolved while the rule runs rather than while it is
        /// declared — a localized resource depends on the culture of the current thread.
        /// </summary>
        public PropertyRuleBuilder<T, TProperty> WithMessage(Func<string> message)
        {
            ArgumentNullException.ThrowIfNull(message);

            this.RequireRule().SetMessageOfLastCheck(message);

            return this;
        }

        /// <summary>
        /// Gives this property a human-readable name for its messages, e.g.
        /// <c>this.Property(x => x.EndDate).WithDisplayName("End date");</c>
        /// </summary>
        /// <remarks>
        /// Opt-in: without it a message names the property by its code, which is its C# name. Applies to
        /// the whole property rather than to one rule, and is picked up by any other rule which compares
        /// against this property — declaring it once is what keeps the two in step. The error code is
        /// unaffected, so callers keep binding messages to inputs by the C# property name.
        /// </remarks>
        public PropertyRuleBuilder<T, TProperty> WithDisplayName(string displayName)
        {
            ArgumentNullException.ThrowIfNull(displayName);

            return this.WithDisplayName(() => displayName);
        }

        /// <summary>
        /// The same, for a name which has to be resolved while the rules run rather than while they are
        /// declared — a localized resource depends on the culture of the current thread.
        /// </summary>
        public PropertyRuleBuilder<T, TProperty> WithDisplayName(Func<string> displayName)
        {
            ArgumentNullException.ThrowIfNull(displayName);

            this.RequireRule().DisplayName = displayName;

            return this;
        }

        /// <summary>
        /// Reports this property's failures under <paramref name="errorCode"/> instead of its member
        /// path, e.g. <c>this.Property(x =&gt; x.Model.Manufacturer.Name).WithErrorCode("manufacturer");</c>
        /// </summary>
        /// <remarks>
        /// The code is the token a caller binds a message to, and the member path is only its default.
        /// Override it where the client's field is not shaped like the model's — a flattened form, or a
        /// name the contract froze before the model was refactored. Applies to the whole property, like
        /// <see cref="WithDisplayName(string)"/>, and does not affect the wording of any message.
        /// <para>
        /// A rule which reports under a code of its own — one error per collection entry, say — keeps
        /// that code; this replaces what the property's own rules report under.
        /// </para>
        /// </remarks>
        public PropertyRuleBuilder<T, TProperty> WithErrorCode(string errorCode)
        {
            ArgumentNullException.ThrowIfNull(errorCode);

            this.RequireRule().ErrorCode = errorCode;

            return this;
        }

        /// <summary>
        /// Applies this property's rules only when <paramref name="condition"/> holds, e.g.
        /// <c>this.Property(x => x.Discount).GreaterThan(0).When(x => x.HasDiscount);</c>
        /// </summary>
        /// <remarks>
        /// The condition covers the <b>whole chain</b> no matter where it is written, and the property
        /// is not even read when it does not hold. Calling it more than once combines the conditions.
        /// </remarks>
        public PropertyRuleBuilder<T, TProperty> When(Func<T, bool> condition)
        {
            ArgumentNullException.ThrowIfNull(condition);

            this.RequireRule().AddCondition(condition);

            return this;
        }

        /// <summary>
        /// The inverse of <see cref="When"/>: applies this property's rules unless
        /// <paramref name="condition"/> holds.
        /// </summary>
        public PropertyRuleBuilder<T, TProperty> Unless(Func<T, bool> condition)
        {
            ArgumentNullException.ThrowIfNull(condition);

            return this.When(instance => !condition(instance));
        }

        /// <summary>
        /// A builder is only meaningful when it came from <see cref="Validator{T}.Property{TProperty}"/>.
        /// It is a struct, so a caller can also write <c>default</c>, which carries no rule to append to.
        /// </summary>
        /// <summary>
        /// Appends the element rules as an ordinary check on this property. The cast is safe by
        /// construction: the variance conversion that chose this overload is what proves the property
        /// really is a sequence of <typeparamref name="TElement"/>.
        /// </summary>
        void IPropertyRuleTarget<TProperty>.AddElementRule<TElement>(ElementRuleBuilder<TElement> elements)
        {
            // A string is a sequence of characters, so the conversion which selects ForEach accepts one
            // and the rules would run per character. Nobody means that, and the failure would be a
            // baffling pile of errors rather than a compiler complaint.
            if (typeof(TProperty) == typeof(string))
            {
                throw new InvalidOperationException(
                    "ForEach cannot be declared for a string. Rules about the text itself belong on the property.");
            }

            this.RequireRule().Add(async (context, cancellationToken) =>
            {
                if (context.Value is IEnumerable<TElement> sequence)
                {
                    await elements.ValidateElementsAsync(
                        sequence, context.Code, context.AddError, context.Messages, cancellationToken);
                }
            });
        }

        private PropertyRule<T, TProperty> RequireRule()
        {
            return this.rule ?? throw new InvalidOperationException(
                $"A {nameof(PropertyRuleBuilder<T, TProperty>)} must be obtained from {nameof(Validator<T>)}.Property(...).");
        }
    }
}
