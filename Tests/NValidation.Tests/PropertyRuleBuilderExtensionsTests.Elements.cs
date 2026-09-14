namespace NValidation.Tests
{
    /// <summary>
    /// Rules declared for every element of a collection: what they report, and how far they walk the
    /// collection to find out.
    /// </summary>
    public partial class PropertyRuleBuilderExtensionsTests
    {
        [Fact]
        public async Task ForEach_ReportsTheFailureUnderTheElementsPosition()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .ForEach(record => record.Property(r => r.Workshop).NotEmpty());

            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = "Aurora Service" },
                new ServiceRecord { Workshop = null },
            ];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("ServiceHistory[1].Workshop", "Workshop is required.");
        }

        [Fact]
        public async Task ForEach_ReportsEveryFailingElement()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .ForEach(record => record.Property(r => r.Workshop).NotEmpty());

            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = null },
                new ServiceRecord { Workshop = "Aurora Service" },
                new ServiceRecord { Workshop = "  " },
            ];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport([
                new("ServiceHistory[0].Workshop", "Workshop is required."),
                new("ServiceHistory[2].Workshop", "Workshop is required.")]);
        }

        [Fact]
        public async Task ForEach_ReportsTheMessageTheRuleChose()
        {
            // Arrange
            var validator = new TestValidator<Car>(MessageKeyProvider.Instance);
            validator.Property(c => c.ServiceHistory)
                .ForEach(record => record.Property(r => r.Workshop).NotEmpty());

            var car = Cars.Car();
            car.ServiceHistory = [new ServiceRecord { Workshop = null }];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("ServiceHistory[0].Workshop", "NotEmpty");
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        public async Task ForEach_WithNothingToWalk_ReportsNothing(int? entryCount)
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .ForEach(record => record.Property(r => r.Workshop).NotEmpty());

            var car = Cars.Car();
            car.ServiceHistory = entryCount == null ? null : [];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        /// <summary>
        /// A null entry has no properties to judge, and requiring entries to be there at all is a
        /// question about the collection rather than about one of its elements.
        /// </summary>
        [Fact]
        public async Task ForEach_SkipsANullElement_WithoutDisturbingTheIndexes()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .ForEach(record => record.Property(r => r.Workshop).NotEmpty());

            var car = Cars.Car();
            car.ServiceHistory = [null!, new ServiceRecord { Workshop = null }];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("ServiceHistory[1].Workshop", "Workshop is required.");
        }

        [Fact]
        public async Task ForEach_WithAnElementValidator_MergesItsErrorsUnderThePosition()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .ForEach(new ServiceRecordValidator());

            var car = Cars.Car();
            car.ServiceHistory = [new ServiceRecord { Workshop = "Aurora Service", Mileage = 1, Cost = 0m }];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("ServiceHistory[0].Cost", "Cost must be greater than 0.");
        }

        /// <summary>
        /// A skipped element keeps its position, so an index still points at the row the caller sent.
        /// </summary>
        [Fact]
        public async Task Where_JudgesOnlyTheElementsItAccepts_AndLeavesTheIndexesAlone()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .ForEach(record => record
                    .Where(r => r.Cost > 0m)
                    .Property(r => r.Workshop).NotEmpty());

            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = null, Cost = 0m },
                new ServiceRecord { Workshop = null, Cost = 120m },
            ];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("ServiceHistory[1].Workshop", "Workshop is required.");
        }

        [Fact]
        public async Task ErrorCode_OnACollection_ReplacesThePathButKeepsTheIndexAndTheProperty()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .WithErrorCode("history")
                .ForEach(record => record.Property(r => r.Workshop).NotEmpty());

            var car = Cars.Car();
            car.ServiceHistory = [new ServiceRecord { Workshop = null }];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("history[0].Workshop", "Workshop is required.");
        }

        /// <summary>
        /// A property declared as a bare sequence may be a query or a one-shot iterator, so the element
        /// rules have to walk it exactly once.
        /// </summary>
        [Fact]
        public async Task ForEach_OnALazySequence_WalksItExactlyOnce()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceMileages)
                .ForEach(mileage => mileage.Element().GreaterThanOrEqualTo(0));

            var sequence = new CountingSequence([3, -1, 7]);
            var car = Cars.Car();
            car.ServiceMileages = sequence;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("ServiceMileages[1]", "ServiceMileages[1] must be greater than or equal to 0.");
            sequence.Enumerated.Should().Be(3);
        }

        /// <summary>
        /// A message about one entry can name the row it is about, which is the only way the reader
        /// learns the position without reading the error code.
        /// </summary>
        [Fact]
        public async Task ForEach_OffersTheElementsPosition_AsAMessagePlaceholder()
        {
            // Arrange
            var validator = new TestValidator<Car>
            {
                Messages = new TemplateMessageProvider("Entry {CollectionIndex} is incomplete."),
            };
            validator.Property(c => c.ServiceHistory)
                .ForEach(record => record.Property(r => r.Workshop).NotEmpty());

            var car = Cars.Car();
            car.ServiceHistory = [new ServiceRecord { Workshop = "Aurora" }, new ServiceRecord { Workshop = null }];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("ServiceHistory[1].Workshop", "Entry 1 is incomplete.");
        }

        /// <summary>
        /// Resolves every message to one template, so a test can prove which placeholders a rule makes
        /// available.
        /// </summary>
        /// <summary>
        /// The entries' own validator answers through the run's provider, not its own — so a message
        /// about an entry can name the entry's position whichever way the rules were declared.
        /// </summary>
        [Fact]
        public async Task ForEach_WithAnElementValidator_ResolvesMessagesThroughTheRunsProvider()
        {
            // Arrange
            var validator = new TestValidator<Car>
            {
                Messages = new TemplateMessageProvider("Entry {CollectionIndex} is incomplete."),
            };
            validator.Property(c => c.ServiceHistory)
                .ForEach(new ServiceRecordValidator());

            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = "Aurora", Mileage = 1_000, Cost = 120m },
                new ServiceRecord { Workshop = null, Mileage = 2_000, Cost = 90m },
            ];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("ServiceHistory[1].Workshop", "Entry 1 is incomplete.");
        }

        private sealed class TemplateMessageProvider(string template) : IValidationMessageProvider
        {
            public string GetMessage(string messageKey, IReadOnlyDictionary<string, object?> arguments)
            {
                return ValidationMessageFormatter.Format(template, arguments);
            }
        }

        /// <summary>
        /// A condition on an element's rule chain is asked about the element, not about the object the
        /// collection hangs off.
        /// </summary>
        [Fact]
        public async Task ForEach_WhenOnAnElementChain_JudgesOnlyTheElementsTheConditionAccepts()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .ForEach(record => record
                    .Property(r => r.Cost).GreaterThan(0m).When(r => r.Mileage > 0));

            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = "Aurora", Mileage = 1_000, Cost = 0m },
                new ServiceRecord { Workshop = "Northgate", Mileage = 0, Cost = 0m },
            ];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("ServiceHistory[0].Cost", "Cost must be greater than 0.");
        }

        /// <summary>
        /// The inverse reads the other way round but reaches the same element.
        /// </summary>
        [Fact]
        public async Task ForEach_UnlessOnAnElementChain_SkipsTheElementsTheConditionAccepts()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .ForEach(record => record
                    .Property(r => r.Cost).GreaterThan(0m).Unless(r => r.Mileage == 0));

            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = "Aurora", Mileage = 0, Cost = 0m },
                new ServiceRecord { Workshop = "Northgate", Mileage = 1_000, Cost = 0m },
            ];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("ServiceHistory[1].Cost", "Cost must be greater than 0.");
        }

        /// <summary>
        /// A condition narrows one property of the entry, not the entry as a whole: the other properties
        /// are still judged.
        /// </summary>
        [Fact]
        public async Task ForEach_WhenOnOnePropertyOfAnElement_LeavesTheOtherPropertiesAlone()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .ForEach(record =>
                {
                    record.Property(r => r.Cost).GreaterThan(0m).When(r => r.Mileage > 0);
                    record.Property(r => r.Workshop).NotEmpty();
                });

            var car = Cars.Car();
            car.ServiceHistory = [new ServiceRecord { Workshop = null, Mileage = 0, Cost = 0m }];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("ServiceHistory[0].Workshop", "Workshop is required.");
        }

        /// <summary>
        /// Every property of an entry that breaks a rule is reported, each under its own path.
        /// </summary>
        [Fact]
        public async Task ForEach_ReportsEveryBrokenPropertyOfAnElement()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .ForEach(record =>
                {
                    record.Property(r => r.Workshop).NotEmpty().MaximumLength(20);
                    record.Property(r => r.Mileage).GreaterThan(0);
                    record.Property(r => r.Cost).GreaterThanOrEqualTo(0m);
                });

            var car = Cars.Car();
            car.ServiceHistory = [new ServiceRecord { Workshop = null, Mileage = 0, Cost = -1m }];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport([
                new("ServiceHistory[0].Workshop", "Workshop is required."),
                new("ServiceHistory[0].Mileage", "Mileage must be greater than 0."),
                new("ServiceHistory[0].Cost", "Cost must be greater than or equal to 0.")]);
        }

        /// <summary>
        /// One property of one entry, out of several entries each with several rules: the code names
        /// exactly which value the caller has to fix.
        /// </summary>
        [Fact]
        public async Task ForEach_ReportsTheBrokenPropertyOfTheBrokenElementOnly()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .ForEach(record =>
                {
                    record.Property(r => r.Workshop).NotEmpty().MaximumLength(20);
                    record.Property(r => r.Mileage).GreaterThan(0);
                    record.Property(r => r.Cost).GreaterThanOrEqualTo(0m);
                });

            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = "Aurora", Mileage = 10_000, Cost = 120m },
                new ServiceRecord { Workshop = "A workshop with a very long name", Mileage = 20_000, Cost = 90m },
                new ServiceRecord { Workshop = "Northgate", Mileage = 30_000, Cost = 80m },
            ];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("ServiceHistory[1].Workshop", "Workshop must not exceed 20 characters.");
        }

        /// <summary>
        /// A rule on an entry's property may consult the rest of that entry — and only that entry, so
        /// the same values in a different row are judged on their own.
        /// </summary>
        [Fact]
        public async Task ForEach_CrossPropertyRule_JudgesEachElementAgainstItself()
        {
            // Arrange
            const string message = "A paid service has to record the mileage it happened at.";

            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .ForEach(record => record
                    .Property(r => r.Mileage)
                    .Must((r, mileage) => r.Cost == 0m || mileage > 0, message));

            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = "Aurora", Mileage = 0, Cost = 0m },
                new ServiceRecord { Workshop = "Northgate", Mileage = 0, Cost = 90m },
                new ServiceRecord { Workshop = "Southgate", Mileage = 30_000, Cost = 80m },
            ];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("ServiceHistory[1].Mileage", message);
        }

        /// <summary>
        /// A position is only useful while the client still has the list in the same order. Identifying
        /// an element by something of its own survives a reorder.
        /// </summary>
        [Fact]
        public async Task WithIndexer_IdentifiesTheElementByWhateverItReturns()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .ForEach(record => record
                    .WithIndexer((r, _) => r.Workshop ?? "unknown")
                    .Property(r => r.Cost).GreaterThan(0m));

            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = "Aurora", Cost = 120m, Mileage = 1000 },
                new ServiceRecord { Workshop = "Northgate", Cost = 0m, Mileage = 1000 },
            ];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("ServiceHistory[Northgate].Cost", "Cost must be greater than 0.");
        }

        /// <summary>
        /// Only the code changes: a message about the entry still names its position.
        /// </summary>
        [Fact]
        public async Task WithIndexer_LeavesTheCollectionIndexPlaceholderAlone()
        {
            // Arrange
            var validator = new TestValidator<Car>
            {
                Messages = new TemplateMessageProvider("Entry {CollectionIndex} is wrong."),
            };
            validator.Property(c => c.ServiceHistory)
                .ForEach(record => record
                    .WithIndexer((r, _) => r.Workshop ?? "unknown")
                    .Property(r => r.Cost).GreaterThan(0m));

            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = "Aurora", Cost = 120m },
                new ServiceRecord { Workshop = "Northgate", Cost = 0m },
            ];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("ServiceHistory[Northgate].Cost", "Entry 1 is wrong.");
        }

        [Fact]
        public void WithIndexer_WithoutAnIndexer_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();

            // Act
            var act = () => validator.Property(c => c.ServiceHistory)
                .ForEach(record => record
                    .WithIndexer(null!)
                    .Property(r => r.Cost).GreaterThan(0m));

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// The position is handed to the indexer as well, for an identity that reads better one-based.
        /// </summary>
        [Fact]
        public async Task WithIndexer_IsGivenThePosition_AsWellAsTheElement()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .ForEach(record => record
                    .WithIndexer((_, position) => $"row{position + 1}")
                    .Property(r => r.Cost).GreaterThan(0m));

            var car = Cars.Car();
            car.ServiceHistory = [new ServiceRecord { Cost = 120m }, new ServiceRecord { Cost = 0m }];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("ServiceHistory[row2].Cost", "Cost must be greater than 0.");
        }

        /// <summary>
        /// One chain can carry rules about the collection and about its elements, as long as ForEach
        /// comes last — it answers about the elements, so there is nothing to chain onto afterwards.
        /// </summary>
        [Fact]
        public async Task ForEach_RunsAfterTheCollectionsOwnRulesHavePassed()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .NotEmpty()
                .MaximumCount(2)
                .ForEach(new ServiceRecordValidator());

            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = "Aurora", Mileage = 1, Cost = 120m },
                new ServiceRecord { Workshop = null, Mileage = 2, Cost = 90m },
            ];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("ServiceHistory[1].Workshop", "Workshop is required.");
        }

        /// <summary>
        /// And it is a rule like any other, so a chain which has already failed does not reach it: too
        /// many entries is reported on its own rather than alongside a complaint about each of them.
        /// </summary>
        [Fact]
        public async Task ForEach_IsNotReached_WhenAnEarlierRuleInTheChainFailed()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .NotEmpty()
                .MaximumCount(2)
                .ForEach(new ServiceRecordValidator());

            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = "Aurora", Mileage = 1, Cost = 120m },
                new ServiceRecord { Workshop = "Northgate", Mileage = 2, Cost = 90m },
                new ServiceRecord { Workshop = null, Mileage = 3, Cost = 80m },
            ];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("ServiceHistory", "ServiceHistory must not contain more than 2 entries.");
        }

        /// <summary>
        /// Every entry is a run of its own, so an entry told to stop at its first error reports one
        /// message — and the entry after it is still judged. The setting governs what one entry says,
        /// never how many entries are looked at.
        /// </summary>
        [Fact]
        public async Task ForEach_StoppingAtTheFirstError_StopsWithinAnEntryAndStillJudgesTheNext()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .ForEach(record =>
                {
                    record.ValidationBehaviors.Class = ValidationBehavior.StopAtFirstError;

                    record.Property(r => r.Workshop).NotEmpty();
                    record.Property(r => r.Cost).GreaterThan(0m);
                });

            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = null, Mileage = 1, Cost = 0m },
                new ServiceRecord { Workshop = null, Mileage = 2, Cost = 0m },
            ];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport([
                new("ServiceHistory[0].Workshop", "Workshop is required."),
                new("ServiceHistory[1].Workshop", "Workshop is required.")]);
        }

        /// <summary>
        /// An entry's own validator counts only what it reported, not what was already in the list it
        /// was handed. The list is shared with the inline rules, which have already had their say by
        /// the time the entry's validator runs — a stopping validator that counted the whole list would
        /// give up before judging anything.
        /// </summary>
        [Fact]
        public async Task ForEach_AStoppingElementValidator_JudgesTheEntryAnInlineRuleAlreadyReportedOn()
        {
            // Arrange
            var recordValidator = new TestValidator<ServiceRecord>();
            recordValidator.ValidationBehaviors.Class = ValidationBehavior.StopAtFirstError;
            recordValidator.Property(r => r.Cost).GreaterThan(0m);

            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .ForEach(record =>
                {
                    record.SetValidator(recordValidator);

                    record.Property(r => r.Workshop).NotEmpty();
                });

            var car = Cars.Car();
            car.ServiceHistory = [new ServiceRecord { Workshop = null, Mileage = 1, Cost = 0m }];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport([
                new("ServiceHistory[0].Workshop", "Workshop is required."),
                new("ServiceHistory[0].Cost", "Cost must be greater than 0.")]);
        }

        /// <inheritdoc cref="ValidatorTests.ValidateAsync_StoppingAtTheFirstError_DoesNotTruncateWhatOneRuleReported" path="/summary"/>
        /// <remarks>
        /// The collection case of the same thing: ForEach is one rule, so every entry it walked is
        /// reported even by a run which stops at the first error.
        /// </remarks>
        [Fact]
        public async Task ForEach_UnderAStoppingRun_StillReportsOnEveryEntry()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.ValidationBehaviors.Class = ValidationBehavior.StopAtFirstError;

            validator.Property(c => c.ServiceHistory)
                .ForEach(record => record.Property(r => r.Workshop).NotEmpty());

            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = null, Mileage = 1, Cost = 10m },
                new ServiceRecord { Workshop = null, Mileage = 2, Cost = 20m },
            ];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport([
                new("ServiceHistory[0].Workshop", "Workshop is required."),
                new("ServiceHistory[1].Workshop", "Workshop is required.")]);
        }

        /// <summary>
        /// A string satisfies the sequence conversion that selects ForEach, so the mistake has to be
        /// caught when the rule is declared rather than becoming one failure per character.
        /// </summary>
        [Fact]
        public void ForEach_OnAString_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();

            // Act
            var act = () => validator.Property(c => c.Vin).ForEach(character => character.Element().NotDefault());

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("*string*");
        }
    }
}
