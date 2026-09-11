namespace NValidation.TestData.Validators
{
    internal sealed class FeatureIdsNoDuplicatesValidator : Validator<Car>
    {
        public FeatureIdsNoDuplicatesValidator()
        {
            this.Property(c => c.FeatureIds).NoDuplicates();
        }
    }

    /// <inheritdoc cref="PreviousOwnerIdsNotEmptyValidator"/>
    internal sealed class PreviousOwnerIdsNoDuplicatesValidator : Validator<Car>
    {
        public PreviousOwnerIdsNoDuplicatesValidator()
        {
            this.Property(c => c.PreviousOwnerIds).NoDuplicates();
        }
    }

    internal sealed class FeatureIdsMinimumCountValidator : Validator<Car>
    {
        public FeatureIdsMinimumCountValidator(int minimumCount)
        {
            this.Property(c => c.FeatureIds).MinimumCount(minimumCount);
        }
    }

    internal sealed class FeatureIdsMaximumCountValidator : Validator<Car>
    {
        public FeatureIdsMaximumCountValidator(int maximumCount)
        {
            this.Property(c => c.FeatureIds).MaximumCount(maximumCount);
        }
    }

    /// <summary>
    /// Declared against a bare sequence, so the collection rules are exercised on something that is not
    /// an <see cref="System.Collections.ICollection"/> and cannot report its own size.
    /// </summary>
    internal sealed class ServiceMileagesNotEmptyValidator : Validator<Car>
    {
        public ServiceMileagesNotEmptyValidator()
        {
            this.Property(c => c.ServiceMileages).NotEmpty();
        }
    }

    /// <inheritdoc cref="ServiceMileagesNotEmptyValidator"/>
    internal sealed class ServiceMileagesMaximumCountValidator : Validator<Car>
    {
        public ServiceMileagesMaximumCountValidator(int maximumCount)
        {
            this.Property(c => c.ServiceMileages).MaximumCount(maximumCount);
        }
    }

    /// <summary>
    /// Rules declared against each entry of the service history.
    /// </summary>
    internal sealed class ServiceHistoryElementValidator : Validator<Car>
    {
        public ServiceHistoryElementValidator()
        {
            this.Property(c => c.ServiceHistory)
                .ForEach(record => record.Property(r => r.Workshop).NotEmpty());
        }
    }

    /// <summary>
    /// The element's own validator, merged per entry.
    /// </summary>
    internal sealed class ServiceHistoryNestedValidator : Validator<Car>
    {
        public ServiceHistoryNestedValidator()
        {
            this.Property(c => c.ServiceHistory)
                .ForEach(new ServiceRecordValidator());
        }
    }

    /// <summary>
    /// Only the entries the condition accepts are judged; the rest keep their position.
    /// </summary>
    internal sealed class ServiceHistoryPaidElementValidator : Validator<Car>
    {
        public ServiceHistoryPaidElementValidator()
        {
            this.Property(c => c.ServiceHistory)
                .ForEach(record => record
                    .Where(r => r.Cost > 0m)
                    .Property(r => r.Workshop).NotEmpty());
        }
    }

    /// <summary>
    /// The collection reports under a code of its own, which the index and the element's property still
    /// follow.
    /// </summary>
    internal sealed class ServiceHistoryErrorCodeElementValidator : Validator<Car>
    {
        public ServiceHistoryErrorCodeElementValidator()
        {
            this.Property(c => c.ServiceHistory)
                .WithErrorCode("history")
                .ForEach(record => record.Property(r => r.Workshop).NotEmpty());
        }
    }

    /// <summary>
    /// Declared against a bare sequence of scalars, to prove an element rule walks a lazy collection
    /// once and no further than it must.
    /// </summary>
    internal sealed class ServiceMileagesElementValidator : Validator<Car>
    {
        public ServiceMileagesElementValidator()
        {
            this.Property(c => c.ServiceMileages)
                .ForEach(mileage => mileage.Element().GreaterThanOrEqualTo(0));
        }
    }

    /// <summary>
    /// An element rule which only applies to some entries, decided by the entry itself rather than by
    /// <see cref="ElementRuleBuilder{TElement}.Where"/> — the entries it skips are still validated by
    /// every other rule declared for them.
    /// </summary>
    internal sealed class ServiceHistoryConditionalElementValidator : Validator<Car>
    {
        public ServiceHistoryConditionalElementValidator()
        {
            this.Property(c => c.ServiceHistory)
                .ForEach(record => record
                    .Property(r => r.Cost).GreaterThan(0m).When(r => r.Mileage > 0));
        }
    }

    /// <summary>
    /// One property of the entry judged only under a condition, next to one judged always — so a
    /// skipped chain can be told apart from a skipped entry.
    /// </summary>
    internal sealed class ServiceHistoryConditionalAndRequiredValidator : Validator<Car>
    {
        public ServiceHistoryConditionalAndRequiredValidator()
        {
            this.Property(c => c.ServiceHistory)
                .ForEach(record =>
                {
                    record.Property(r => r.Cost).GreaterThan(0m).When(r => r.Mileage > 0);
                    record.Property(r => r.Workshop).NotEmpty();
                });
        }
    }

    /// <summary>
    /// The inverse condition, to prove <c>Unless</c> reaches the element rather than the object the
    /// collection hangs off.
    /// </summary>
    internal sealed class ServiceHistoryUnlessElementValidator : Validator<Car>
    {
        public ServiceHistoryUnlessElementValidator()
        {
            this.Property(c => c.ServiceHistory)
                .ForEach(record => record
                    .Property(r => r.Cost).GreaterThan(0m).Unless(r => r.Mileage == 0));
        }
    }

    /// <summary>
    /// Several properties of one entry, each with a chain of its own. Declared as a block because a
    /// chain belongs to the property it started on and cannot be continued onto the next one.
    /// </summary>
    internal sealed class ServiceHistoryMultiPropertyValidator : Validator<Car>
    {
        public ServiceHistoryMultiPropertyValidator()
        {
            this.Property(c => c.ServiceHistory)
                .ForEach(record =>
                {
                    record.Property(r => r.Workshop).NotEmpty().MaximumLength(20);
                    record.Property(r => r.Mileage).GreaterThan(0);
                    record.Property(r => r.Cost).GreaterThanOrEqualTo(0m);
                });
        }
    }

    /// <summary>
    /// A rule on one property of an entry which needs another property of the same entry to decide.
    /// </summary>
    internal sealed class ServiceHistoryCrossPropertyValidator : Validator<Car>
    {
        internal const string Message = "A paid service has to record the mileage it happened at.";

        public ServiceHistoryCrossPropertyValidator()
        {
            this.Property(c => c.ServiceHistory)
                .ForEach(record => record
                    .Property(r => r.Mileage)
                    .Must((r, mileage) => r.Cost == 0m || mileage > 0, Message));
        }
    }

    /// <summary>
    /// Elements identified by something of their own instead of by position.
    /// </summary>
    internal sealed class ServiceHistoryIndexedElementValidator : Validator<Car>
    {
        public ServiceHistoryIndexedElementValidator()
        {
            this.Property(c => c.ServiceHistory)
                .ForEach(record => record
                    .WithIndexer((r, _) => r.Workshop ?? "unknown")
                    .Property(r => r.Cost).GreaterThan(0m));
        }
    }

    /// <inheritdoc cref="ServiceHistoryIndexedElementValidator"/>
    internal sealed class ServiceHistoryCustomIndexerValidator : Validator<Car>
    {
        public ServiceHistoryCustomIndexerValidator(Func<ServiceRecord, int, string> indexer)
        {
            this.Property(c => c.ServiceHistory)
                .ForEach(record => record
                    .WithIndexer(indexer)
                    .Property(r => r.Cost).GreaterThan(0m));
        }
    }

    /// <summary>
    /// Declares element rules on a string, which is a sequence of characters and would otherwise be
    /// judged one character at a time.
    /// </summary>
    internal sealed class VinForEachValidator : Validator<Car>
    {
        public VinForEachValidator()
        {
            this.Property(c => c.Vin).ForEach(character => character.Element().NotDefault());
        }
    }

    /// <summary>
    /// Collection rules and element rules declared on one chain.
    /// </summary>
    internal sealed class ServiceHistoryBoundedAndCheckedValidator : Validator<Car>
    {
        public ServiceHistoryBoundedAndCheckedValidator()
        {
            this.Property(c => c.ServiceHistory)
                .NotEmpty()
                .MaximumCount(2)
                .ForEach(new ServiceRecordValidator());
        }
    }

    /// <summary>
    /// A stopping run per entry: an entry reports the first thing wrong with it and no more, while the
    /// entry after it is still judged — every entry is a run of its own.
    /// </summary>
    internal sealed class ServiceHistoryStoppingElementValidator : Validator<Car>
    {
        public ServiceHistoryStoppingElementValidator()
        {
            this.Property(c => c.ServiceHistory)
                .ForEach(record =>
                {
                    record.ValidationBehaviors.Class = ValidationBehavior.StopAtFirstError;

                    record.Property(r => r.Workshop).NotEmpty();
                    record.Property(r => r.Cost).GreaterThan(0m);
                });
        }
    }

    /// <summary>
    /// Stops at its own first error, and is merged into an entry an inline rule has already reported
    /// on — so it is handed a list which is not empty before it has judged anything.
    /// </summary>
    internal sealed class StoppingServiceRecordValidator : Validator<ServiceRecord>
    {
        public StoppingServiceRecordValidator()
        {
            this.ValidationBehaviors.Class = ValidationBehavior.StopAtFirstError;

            this.Property(r => r.Cost).GreaterThan(0m);
        }
    }

    /// <summary>
    /// An inline element rule and the entry's own stopping validator over the same entries. Both report
    /// into the one list an entry is given, and the inline rule reports first — which is what makes a
    /// run that counted the whole list rather than its own share of it visible.
    /// </summary>
    internal sealed class ServiceHistoryInlineAndStoppingNestedValidator : Validator<Car>
    {
        public ServiceHistoryInlineAndStoppingNestedValidator()
        {
            this.Property(c => c.ServiceHistory)
                .ForEach(record =>
                {
                    record.SetValidator(new StoppingServiceRecordValidator());

                    record.Property(r => r.Workshop).NotEmpty();
                });
        }
    }

    /// <summary>
    /// A stopping run whose only rule walks a collection. Every entry is judged by the one rule, so the
    /// run has nothing to stop between and reports on all of them.
    /// </summary>
    internal sealed class StoppingRunOverAServiceHistoryValidator : Validator<Car>
    {
        public StoppingRunOverAServiceHistoryValidator()
        {
            this.ValidationBehaviors.Class = ValidationBehavior.StopAtFirstError;

            this.Property(c => c.ServiceHistory)
                .ForEach(record => record.Property(r => r.Workshop).NotEmpty());
        }
    }

    /// <summary>
    /// Element rules with nothing declared about their behaviour, so a test can prove what an element
    /// builder falls back to when the registration configured something the parent did receive.
    /// </summary>
    internal sealed class ServiceHistoryPlainElementChainValidator : Validator<Car>
    {
        public ServiceHistoryPlainElementChainValidator()
        {
            this.Property(c => c.ServiceHistory)
                .ForEach(record => record.Property(r => r.Workshop).NotEmpty().MaximumLength(3));
        }
    }
}
