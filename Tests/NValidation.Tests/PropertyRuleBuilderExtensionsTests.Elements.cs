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
            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = "Aurora Service" },
                new ServiceRecord { Workshop = null },
            ];

            // Act
            var result = await new ServiceHistoryElementValidator().ValidateAsync(car);

            // Assert
            result.Errors.Should().ContainSingle().Which.Code.Should().Be("ServiceHistory[1].Workshop");
        }

        [Fact]
        public async Task ForEach_ReportsEveryFailingElement()
        {
            // Arrange
            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = null },
                new ServiceRecord { Workshop = "Aurora Service" },
                new ServiceRecord { Workshop = "  " },
            ];

            // Act
            var result = await new ServiceHistoryElementValidator().ValidateAsync(car);

            // Assert
            result.Errors.Select(error => error.Code).Should()
                .BeEquivalentTo(["ServiceHistory[0].Workshop", "ServiceHistory[2].Workshop"]);
        }

        [Fact]
        public async Task ForEach_ReportsTheMessageTheRuleChose()
        {
            // Arrange
            var car = Cars.Car();
            car.ServiceHistory = [new ServiceRecord { Workshop = null }];

            // Act
            var result = await new ServiceHistoryElementValidator().ValidateForKeysAsync(car);

            // Assert
            result.ShouldReport("ServiceHistory[0].Workshop", ValidationMessageKeys.NotEmpty);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        public async Task ForEach_WithNothingToWalk_ReportsNothing(int? entryCount)
        {
            // Arrange
            var car = Cars.Car();
            car.ServiceHistory = entryCount == null ? null : [];

            // Act
            var result = await new ServiceHistoryElementValidator().ValidateAsync(car);

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
            var car = Cars.Car();
            car.ServiceHistory = [null!, new ServiceRecord { Workshop = null }];

            // Act
            var result = await new ServiceHistoryElementValidator().ValidateAsync(car);

            // Assert
            result.Errors.Should().ContainSingle().Which.Code.Should().Be("ServiceHistory[1].Workshop");
        }

        [Fact]
        public async Task ForEach_WithAnElementValidator_MergesItsErrorsUnderThePosition()
        {
            // Arrange
            var car = Cars.Car();
            car.ServiceHistory = [new ServiceRecord { Workshop = "Aurora Service", Mileage = 1, Cost = 0m }];

            // Act
            var result = await new ServiceHistoryNestedValidator().ValidateAsync(car);

            // Assert
            result.Errors.Should().ContainSingle().Which.Code.Should().Be("ServiceHistory[0].Cost");
        }

        /// <summary>
        /// A skipped element keeps its position, so an index still points at the row the caller sent.
        /// </summary>
        [Fact]
        public async Task Where_JudgesOnlyTheElementsItAccepts_AndLeavesTheIndexesAlone()
        {
            // Arrange
            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = null, Cost = 0m },
                new ServiceRecord { Workshop = null, Cost = 120m },
            ];

            // Act
            var result = await new ServiceHistoryPaidElementValidator().ValidateAsync(car);

            // Assert
            result.Errors.Should().ContainSingle().Which.Code.Should().Be("ServiceHistory[1].Workshop");
        }

        [Fact]
        public async Task ErrorCode_OnACollection_ReplacesThePathButKeepsTheIndexAndTheProperty()
        {
            // Arrange
            var car = Cars.Car();
            car.ServiceHistory = [new ServiceRecord { Workshop = null }];

            // Act
            var result = await new ServiceHistoryErrorCodeElementValidator().ValidateAsync(car);

            // Assert
            result.Errors.Should().ContainSingle().Which.Code.Should().Be("history[0].Workshop");
        }

        /// <summary>
        /// A property declared as a bare sequence may be a query or a one-shot iterator, so the element
        /// rules have to walk it exactly once.
        /// </summary>
        [Fact]
        public async Task ForEach_OnALazySequence_WalksItExactlyOnce()
        {
            // Arrange
            var sequence = new CountingSequence([3, -1, 7]);
            var car = Cars.Car();
            car.ServiceMileages = sequence;

            // Act
            var result = await new ServiceMileagesElementValidator().ValidateAsync(car);

            // Assert
            result.Errors.Should().ContainSingle().Which.Code.Should().Be("ServiceMileages[1]");
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
            var car = Cars.Car();
            car.ServiceHistory = [new ServiceRecord { Workshop = "Aurora" }, new ServiceRecord { Workshop = null }];

            var validator = new ServiceHistoryElementValidator
            {
                Messages = new TemplateMessageProvider("Entry {CollectionIndex} is incomplete."),
            };

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Errors.Should().ContainSingle().Which.Message.Should().Be("Entry 1 is incomplete.");
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
            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = "Aurora", Mileage = 1_000, Cost = 120m },
                new ServiceRecord { Workshop = null, Mileage = 2_000, Cost = 90m },
            ];

            var validator = new ServiceHistoryNestedValidator
            {
                Messages = new TemplateMessageProvider("Entry {CollectionIndex} is incomplete."),
            };

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            var error = result.Errors.Should().ContainSingle().Subject;
            error.Code.Should().Be("ServiceHistory[1].Workshop");
            error.Message.Should().Be("Entry 1 is incomplete.");
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
            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = "Aurora", Mileage = 1_000, Cost = 0m },
                new ServiceRecord { Workshop = "Northgate", Mileage = 0, Cost = 0m },
            ];

            // Act
            var result = await new ServiceHistoryConditionalElementValidator().ValidateAsync(car);

            // Assert
            result.Errors.Should().ContainSingle().Which.Code.Should().Be("ServiceHistory[0].Cost");
        }

        /// <summary>
        /// The inverse reads the other way round but reaches the same element.
        /// </summary>
        [Fact]
        public async Task ForEach_UnlessOnAnElementChain_SkipsTheElementsTheConditionAccepts()
        {
            // Arrange
            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = "Aurora", Mileage = 0, Cost = 0m },
                new ServiceRecord { Workshop = "Northgate", Mileage = 1_000, Cost = 0m },
            ];

            // Act
            var result = await new ServiceHistoryUnlessElementValidator().ValidateAsync(car);

            // Assert
            result.Errors.Should().ContainSingle().Which.Code.Should().Be("ServiceHistory[1].Cost");
        }

        /// <summary>
        /// A condition narrows one property of the entry, not the entry as a whole: the other properties
        /// are still judged.
        /// </summary>
        [Fact]
        public async Task ForEach_WhenOnOnePropertyOfAnElement_LeavesTheOtherPropertiesAlone()
        {
            // Arrange
            var car = Cars.Car();
            car.ServiceHistory = [new ServiceRecord { Workshop = null, Mileage = 0, Cost = 0m }];

            // Act
            var result = await new ServiceHistoryConditionalAndRequiredValidator().ValidateAsync(car);

            // Assert
            result.Errors.Should().ContainSingle().Which.Code.Should().Be("ServiceHistory[0].Workshop");
        }

        /// <summary>
        /// Every property of an entry that breaks a rule is reported, each under its own path.
        /// </summary>
        [Fact]
        public async Task ForEach_ReportsEveryBrokenPropertyOfAnElement()
        {
            // Arrange
            var car = Cars.Car();
            car.ServiceHistory = [new ServiceRecord { Workshop = null, Mileage = 0, Cost = -1m }];

            // Act
            var result = await new ServiceHistoryMultiPropertyValidator().ValidateAsync(car);

            // Assert
            result.Errors.Select(error => error.Code).Should().BeEquivalentTo(
                ["ServiceHistory[0].Workshop", "ServiceHistory[0].Mileage", "ServiceHistory[0].Cost"]);
        }

        /// <summary>
        /// One property of one entry, out of several entries each with several rules: the code names
        /// exactly which value the caller has to fix.
        /// </summary>
        [Fact]
        public async Task ForEach_ReportsTheBrokenPropertyOfTheBrokenElementOnly()
        {
            // Arrange
            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = "Aurora", Mileage = 10_000, Cost = 120m },
                new ServiceRecord { Workshop = "A workshop with a very long name", Mileage = 20_000, Cost = 90m },
                new ServiceRecord { Workshop = "Northgate", Mileage = 30_000, Cost = 80m },
            ];

            // Act
            var result = await new ServiceHistoryMultiPropertyValidator().ValidateAsync(car);

            // Assert
            result.Errors.Should().ContainSingle().Which.Code.Should().Be("ServiceHistory[1].Workshop");
        }

        /// <summary>
        /// A rule on an entry's property may consult the rest of that entry — and only that entry, so
        /// the same values in a different row are judged on their own.
        /// </summary>
        [Fact]
        public async Task ForEach_CrossPropertyRule_JudgesEachElementAgainstItself()
        {
            // Arrange
            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = "Aurora", Mileage = 0, Cost = 0m },
                new ServiceRecord { Workshop = "Northgate", Mileage = 0, Cost = 90m },
                new ServiceRecord { Workshop = "Southgate", Mileage = 30_000, Cost = 80m },
            ];

            // Act
            var result = await new ServiceHistoryCrossPropertyValidator().ValidateAsync(car);

            // Assert
            var error = result.Errors.Should().ContainSingle().Subject;
            error.Code.Should().Be("ServiceHistory[1].Mileage");
            error.Message.Should().Be(ServiceHistoryCrossPropertyValidator.Message);
        }

        /// <summary>
        /// A position is only useful while the client still has the list in the same order. Identifying
        /// an element by something of its own survives a reorder.
        /// </summary>
        [Fact]
        public async Task WithIndexer_IdentifiesTheElementByWhateverItReturns()
        {
            // Arrange
            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = "Aurora", Cost = 120m, Mileage = 1000 },
                new ServiceRecord { Workshop = "Northgate", Cost = 0m, Mileage = 1000 },
            ];

            // Act
            var result = await new ServiceHistoryIndexedElementValidator().ValidateAsync(car);

            // Assert
            result.Errors.Should().HaveCount(1);
            result.Errors.Should().ContainSingle().Which.Code.Should().Be("ServiceHistory[Northgate].Cost");
        }

        /// <summary>
        /// Only the code changes: a message about the entry still names its position.
        /// </summary>
        [Fact]
        public async Task WithIndexer_LeavesTheCollectionIndexPlaceholderAlone()
        {
            // Arrange
            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = "Aurora", Cost = 120m },
                new ServiceRecord { Workshop = "Northgate", Cost = 0m },
            ];

            var validator = new ServiceHistoryIndexedElementValidator
            {
                Messages = new TemplateMessageProvider("Entry {CollectionIndex} is wrong."),
            };

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Errors.Should().ContainSingle().Which.Message.Should().Be("Entry 1 is wrong.");
        }

        [Fact]
        public void WithIndexer_WithoutAnIndexer_Throws()
        {
            // Act
            var act = () => new ServiceHistoryCustomIndexerValidator(null!);

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
            var car = Cars.Car();
            car.ServiceHistory = [new ServiceRecord { Cost = 120m }, new ServiceRecord { Cost = 0m }];

            var validator = new ServiceHistoryCustomIndexerValidator((_, position) => $"row{position + 1}");

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Errors.Should().ContainSingle().Which.Code.Should().Be("ServiceHistory[row2].Cost");
        }

        /// <summary>
        /// One chain can carry rules about the collection and about its elements, as long as ForEach
        /// comes last — it answers about the elements, so there is nothing to chain onto afterwards.
        /// </summary>
        [Fact]
        public async Task ForEach_RunsAfterTheCollectionsOwnRulesHavePassed()
        {
            // Arrange
            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = "Aurora", Mileage = 1, Cost = 120m },
                new ServiceRecord { Workshop = null, Mileage = 2, Cost = 90m },
            ];

            // Act
            var result = await new ServiceHistoryBoundedAndCheckedValidator().ValidateAsync(car);

            // Assert
            result.Errors.Should().ContainSingle().Which.Code.Should().Be("ServiceHistory[1].Workshop");
        }

        /// <summary>
        /// And it is a rule like any other, so a chain which has already failed does not reach it: too
        /// many entries is reported on its own rather than alongside a complaint about each of them.
        /// </summary>
        [Fact]
        public async Task ForEach_IsNotReached_WhenAnEarlierRuleInTheChainFailed()
        {
            // Arrange
            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = "Aurora", Mileage = 1, Cost = 120m },
                new ServiceRecord { Workshop = "Northgate", Mileage = 2, Cost = 90m },
                new ServiceRecord { Workshop = null, Mileage = 3, Cost = 80m },
            ];

            // Act
            var result = await new ServiceHistoryBoundedAndCheckedValidator().ValidateAsync(car);

            // Assert
            result.Errors.Should().ContainSingle().Which.Code.Should().Be(nameof(Car.ServiceHistory));
        }

        /// <summary>
        /// A string satisfies the sequence conversion that selects ForEach, so the mistake has to be
        /// caught when the rule is declared rather than becoming one failure per character.
        /// </summary>
        [Fact]
        public void ForEach_OnAString_Throws()
        {
            // Act
            var act = () => new VinForEachValidator();

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("*string*");
        }
    }
}
