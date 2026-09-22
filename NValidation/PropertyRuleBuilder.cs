using NValidation.Internals;

namespace NValidation
{
    /// <summary>
    /// The chainable part of <see cref="Validator{T}.Property{TProperty}(System.Linq.Expressions.Expression{System.Func{T, TProperty}})"/>. Rules are extension methods
    /// on this type, so an application can add its own without touching the core.
    /// </summary>
    /// <remarks>
    /// A rule whose refinements belong to it alone returns a builder derived from this one, as
    /// <see cref="PropertyRuleBuilderExtensions.EmailAddress{T}"/> does; every rule written after it still
    /// chains, because the rules are extension methods on the base.
    /// </remarks>
    public class PropertyRuleBuilder<T, TProperty> : IPropertyRuleTarget<TProperty>
    {
        private readonly PropertyRule<T, TProperty> rule;

        internal PropertyRuleBuilder(PropertyRule<T, TProperty> rule)
        {
            this.rule = rule;
        }

        // A derived builder continues the chain of the property it was handed, so it takes over the one
        // thing a builder carries rather than starting a rule of its own.
        internal PropertyRuleBuilder(PropertyRuleBuilder<T, TProperty> source)
            : this(source.rule)
        {
        }

        /// <summary>
        /// Appends a rule to this property's chain.
        /// </summary>
        public PropertyRuleBuilder<T, TProperty> Add(Action<RuleContext<T, TProperty>> check)
        {
            ArgumentNullException.ThrowIfNull(check);

            this.rule.Add(check);

            return this;
        }

        /// <summary>
        /// Appends a rule which needs to await something, e.g. a nested validator.
        /// </summary>
        public PropertyRuleBuilder<T, TProperty> AddAsync(Func<RuleContext<T, TProperty>, CancellationToken, ValueTask> check)
        {
            ArgumentNullException.ThrowIfNull(check);

            this.rule.Add(check);

            return this;
        }

        internal PropertyRuleBuilder<T, TProperty> AddComposed(Func<RuleContext<T, TProperty>, CancellationToken, ValueTask> check)
        {
            ArgumentNullException.ThrowIfNull(check);

            this.rule.AddComposed(check);

            return this;
        }

        internal PropertyRuleBuilder<T, TProperty> AddComposed(Action<RuleContext<T, TProperty>> check)
        {
            ArgumentNullException.ThrowIfNull(check);

            this.rule.AddComposed(check);

            return this;
        }

        /// <summary>
        /// Decides for this one property whether its chain reports every rule it breaks or stops at the
        /// first: <c>this.Property(x => x.Password).MinimumLength(12).Matches("[0-9]").WithValidationBehavior(ValidationBehavior.All);</c>
        /// </summary>
        /// <remarks>
        /// The local exception to what the validator or the registration settled through
        /// <see cref="ValidationBehaviors.Property"/>, and the only thing that outranks a
        /// <see cref="ValidationBehaviors.Class"/> of
        /// <see cref="ValidationBehavior.StopAtFirstError"/> — a run which stops at the first error
        /// still lets this chain finish, then stops. Applies to the whole chain wherever it is written,
        /// not to the rule it happens to follow.
        /// </remarks>
        public PropertyRuleBuilder<T, TProperty> WithValidationBehavior(ValidationBehavior validationBehavior)
        {
            this.rule.ValidationBehaviorOverride = validationBehavior;

            return this;
        }

        /// <summary>
        /// Replaces the message of the rule just written, for the cases where the shared wording does
        /// not fit: <c>this.Property(x => x.Name).NotEmpty().WithMessage("Please tell us your name.");</c>
        /// </summary>
        /// <remarks>
        /// Applies to that one rule, not to the whole chain, so each rule of a property can carry its own
        /// wording. The property name is unaffected.
        /// </remarks>
        public PropertyRuleBuilder<T, TProperty> WithMessage(string message)
        {
            ArgumentNullException.ThrowIfNull(message);

            return this.WithMessage(() => message);
        }

        /// <summary>
        /// Gives the rule just declared a wording of its own, resolved while the rule runs rather than while
        /// it is declared — a localized resource depends on the culture of the current thread.
        /// </summary>
        /// <inheritdoc cref="WithMessage(System.String)" path="/remarks"/>
        public PropertyRuleBuilder<T, TProperty> WithMessage(Func<string> message)
        {
            ArgumentNullException.ThrowIfNull(message);

            return this.WithMessage((_, _) => message());
        }

        /// <summary>
        /// Gives the rule just declared a wording that names something about the object being validated:
        /// <c>this.Property(x => x.Vin).Must(...).WithMessage(car => $"VIN {car.Vin} is already registered.");</c>
        /// </summary>
        /// <remarks>
        /// Applies to that one rule, not to the whole chain. The result is a template like any other spelling
        /// of <c>WithMessage</c>, so it may still name the placeholders the rule supplies.
        /// </remarks>
        public PropertyRuleBuilder<T, TProperty> WithMessage(Func<T, string> message)
        {
            ArgumentNullException.ThrowIfNull(message);

            return this.WithMessage((instance, _) => message(instance));
        }

        /// <summary>
        /// Gives the rule just declared a wording that names the value which failed as well as the object it
        /// came from.
        /// </summary>
        /// <inheritdoc cref="WithMessage(System.Func{T, System.String})" path="/remarks"/>
        public PropertyRuleBuilder<T, TProperty> WithMessage(Func<T, TProperty, string> message)
        {
            ArgumentNullException.ThrowIfNull(message);

            this.rule.SetMessageOfLastCheck(message);

            return this;
        }

        /// <summary>
        /// Names the rule just written, so a client can tell which one failed without reading the
        /// message: <c>this.Property(x => x.Vin).NotEmpty().WithErrorCode("VIN_REQUIRED");</c>
        /// </summary>
        /// <remarks>
        /// Applies to that one rule, not to the whole chain, and does not affect where the failure is
        /// reported — that is <see cref="WithPropertyName(string)"/>'s job. The code is also the key
        /// the message is resolved under, which is what makes a rule of the caller's own localizable:
        /// <c>.Must(...).WithErrorCode("SwissPlate")</c> asks the host's provider for the
        /// <c>SwissPlate</c> text, exactly as a shipped rule asks for its own, and a provider with
        /// nothing under that code answers with the code itself.
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="errorCode"/> is empty or whitespace.</exception>
        public PropertyRuleBuilder<T, TProperty> WithErrorCode(string errorCode)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);

            this.rule.SetErrorCodeOfLastCheck(errorCode);

            return this;
        }

        /// <summary>
        /// Gives this property a human-readable name for its messages, e.g.
        /// <c>this.Property(x => x.EndDate).WithDisplayName("End date");</c>
        /// </summary>
        /// <remarks>
        /// Opt-in: without it a message names the property by its property name, which is its C# name. Applies to
        /// the whole property rather than to one rule, and is picked up by any other rule which compares
        /// against this property — declaring it once is what keeps the two in step. The property name is
        /// unaffected, so callers keep binding messages to inputs by the C# property name.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// <paramref name="displayName"/> is empty or whitespace. A message names the property by this,
        /// so a blank one leaves the sentence without a subject.
        /// </exception>
        public PropertyRuleBuilder<T, TProperty> WithDisplayName(string displayName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

            return this.WithDisplayName(() => displayName);
        }

        /// <summary>
        /// Names this property in its messages, resolved while the rules run rather than while they are
        /// declared — a localized resource depends on the culture of the current thread.
        /// </summary>
        /// <inheritdoc cref="WithDisplayName(System.String)" path="/remarks"/>
        public PropertyRuleBuilder<T, TProperty> WithDisplayName(Func<string> displayName)
        {
            ArgumentNullException.ThrowIfNull(displayName);

            this.rule.DisplayName = displayName;

            return this;
        }

        /// <summary>
        /// Reports this property's failures under <paramref name="propertyName"/> instead of its member
        /// path, e.g. <c>this.Property(x =&gt; x.Model.Manufacturer.Name).WithPropertyName("manufacturerName");</c>
        /// </summary>
        /// <remarks>
        /// The property name is the token a caller binds a message to, and the member path is only its
        /// default. Override it where the client's field is not shaped like the model's — a flattened
        /// form, or a property name the contract froze before the model was refactored. Applies to the whole
        /// property, like <see cref="WithDisplayName(string)"/>, and does not affect the wording of any
        /// message. A rule which reports under a property name of its own — one error per collection
        /// entry, say — keeps that property name; this replaces what the property's own rules report
        /// under.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> is empty or whitespace. The property name is the token a
        /// caller binds a message to, and a blank one binds to nothing. The one empty property name this
        /// library reports under is its own: a rule declared for an element itself, which is named by its
        /// position alone.
        /// </exception>
        public PropertyRuleBuilder<T, TProperty> WithPropertyName(string propertyName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

            this.rule.PropertyNameOverride = propertyName;

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

            this.rule.AddCondition(condition);

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
        /// Puts this property's chain in one or more rule groups, so it runs only when a validation
        /// selects one of them:
        /// <c>this.Property(c => c.TradeInValue).NotNull().WithGroup("Create");</c>
        /// </summary>
        /// <remarks>
        /// Covers the <b>whole chain</b> no matter where it is written, as <see cref="When"/> does, and
        /// a chain whose group was not selected is not even read. A chain in no group runs in every
        /// validation, so grouping one chain never switches another off. Calling it again adds further
        /// groups. What a validation selects is
        /// <see cref="NValidationOptions.ValidationGroups"/>; a call which names none runs the chains in
        /// no group alone.
        /// </remarks>
        /// <exception cref="ArgumentException">No group is named, or a name is empty or whitespace.</exception>
        /// <exception cref="InvalidOperationException">This validator has already validated something.</exception>
        public PropertyRuleBuilder<T, TProperty> WithGroup(params ReadOnlySpan<string> groups)
        {
            GroupNames.ThrowIfEmpty(groups, nameof(groups));

            this.rule.AddGroups(groups, nameof(groups));

            return this;
        }

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

            var rule = this.rule;

            if (elements.IsSynchronous)
            {
                rule.AddComposed(context =>
                {
                    if (context.Value is IEnumerable<TElement> sequence)
                    {
                        elements.ValidateElements(sequence, context.PropertyName, context.Frame);
                    }
                });

                return;
            }

            rule.AddComposed(async (context, _) =>
            {
                if (context.Value is IEnumerable<TElement> sequence)
                {
                    await elements.ValidateElementsAsync(sequence, context.PropertyName, context.Frame);
                }
            });
        }

        // For a derived builder's own refinements, which change the rule after it was declared exactly as
        // WithMessage does, and are refused after the first validation for the same reason.
        private protected void ThrowIfFrozen()
        {
            this.rule.ThrowIfFrozen();
        }
    }
}
