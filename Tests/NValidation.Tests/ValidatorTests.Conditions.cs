namespace NValidation.Tests
{
    public partial class ValidatorTests
    {
        [Fact]
        public async Task When_WhereTheConditionHolds_RunsEveryChainDeclaredInside()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.When(c => c.Condition == CarCondition.Used, () =>
            {
                validator.Property(c => c.IntakeCondition).NotNull();
                validator.Property(c => c.ServiceIntervalKm).NotNull();
            });

            // Act
            var result = await validator.ValidateAsync(new Car { Condition = CarCondition.Used });

            // Assert
            result.ShouldReport([
                new("IntakeCondition", "IntakeCondition is required."),
                new("ServiceIntervalKm", "ServiceIntervalKm is required.")]);
        }

        [Fact]
        public async Task When_WhereTheConditionFails_SkipsEveryChainDeclaredInside()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.When(c => c.Condition == CarCondition.Used, () =>
            {
                validator.Property(c => c.IntakeCondition).NotNull();
                validator.Property(c => c.ServiceIntervalKm).NotNull();
            });

            // Act
            var result = await validator.ValidateAsync(new Car { Condition = CarCondition.New });

            // Assert
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task When_LeavesAChainDeclaredAfterTheBlock_Unconditioned()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.When(_ => false, () => validator.Property(c => c.Vin).NotEmpty());
            validator.Property(c => c.RegistrationPlate).NotEmpty();

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.ShouldReport("RegistrationPlate", "RegistrationPlate is required.");
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public async Task When_Nested_SkipsTheChainUnlessBothConditionsHold(bool outer, bool inner)
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.When(_ => outer, () =>
                validator.When(_ => inner, () => validator.Property(c => c.Vin).NotEmpty()));

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task When_Nested_RunsTheChainWhereBothConditionsHold()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.When(_ => true, () =>
                validator.When(_ => true, () => validator.Property(c => c.Vin).NotEmpty()));

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.ShouldReport("Vin", "Vin is required.");
        }

        [Fact]
        public async Task When_WhenTheBlockThrows_LeavesALaterChainUnconditioned()
        {
            // Arrange
            var validator = new TestValidator<Car>();

            try
            {
                validator.When(_ => false, () => throw new InvalidOperationException("declaring failed"));
            }
            catch (InvalidOperationException)
            {
            }

            validator.Property(c => c.Vin).NotEmpty();

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.ShouldReport("Vin", "Vin is required.");
        }

        [Fact]
        public async Task When_InsideAGroup_NeedsTheGroupAndTheCondition()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Group("Listing", () =>
                validator.When(c => c.Condition == CarCondition.Used, () => validator.Property(c => c.IntakeCondition).NotNull()));

            var options = new NValidationOptions { ValidationGroups = "Listing" };

            // Act
            var withoutTheGroup = await validator.ValidateAsync(new Car { Condition = CarCondition.Used });
            var withTheGroup = await validator.ValidateAsync(new Car { Condition = CarCondition.Used }, options);

            // Assert
            withoutTheGroup.Errors.Should().BeEmpty();
            withTheGroup.ShouldReport("IntakeCondition", "IntakeCondition is required.");
        }

        /// <summary>
        /// The guard of a path through an object the payload omitted is asked first, so a block's condition
        /// which reaches through the same object never meets it absent — for a chain declared with a
        /// reachability predicate exactly as for one declared with an expression.
        /// </summary>
        [Fact]
        public async Task When_IsAskedAfterTheReachabilityPredicate()
        {
            // Arrange
            var validator = new ConditionedNamedNestedPropertyCarValidator();

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task When_IsAskedAfterTheGuardOfAnExpression()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.When(c => c.Model!.Name != "Aurora", () => validator.Property(c => c.Model!.Name).NotEmpty());

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task When_IsAskedBeforeTheChainsOwnCondition()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.When(_ => false, () =>
                validator.Property(c => c.Vin)
                    .NotEmpty()
                    .When(_ => throw new InvalidOperationException("asked out of order")));

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task Otherwise_WhereTheConditionFails_RunsItsChains()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator
                .When(c => c.Condition == CarCondition.Used, () => validator.Property(c => c.IntakeCondition).NotNull())
                .Otherwise(() => validator.Property(c => c.RegistrationPlate).NotEmpty());

            // Act
            var result = await validator.ValidateAsync(new Car { Condition = CarCondition.New });

            // Assert
            result.ShouldReport("RegistrationPlate", "RegistrationPlate is required.");
        }

        [Fact]
        public async Task Otherwise_WhereTheConditionHolds_SkipsItsChains()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator
                .When(c => c.Condition == CarCondition.Used, () => validator.Property(c => c.IntakeCondition).NotNull())
                .Otherwise(() => validator.Property(c => c.RegistrationPlate).NotEmpty());

            // Act
            var result = await validator.ValidateAsync(new Car { Condition = CarCondition.Used });

            // Assert
            result.ShouldReport("IntakeCondition", "IntakeCondition is required.");
        }

        [Fact]
        public async Task Unless_WhereTheConditionHolds_SkipsEveryChainDeclaredInside()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Unless(c => c.Condition == CarCondition.New, () => validator.Property(c => c.IntakeCondition).NotNull());

            // Act
            var result = await validator.ValidateAsync(new Car { Condition = CarCondition.New });

            // Assert
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task Unless_Otherwise_RunsItsChainsWhereTheConditionHolds()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator
                .Unless(c => c.Condition == CarCondition.New, () => validator.Property(c => c.IntakeCondition).NotNull())
                .Otherwise(() => validator.Property(c => c.RegistrationPlate).NotEmpty());

            // Act
            var result = await validator.ValidateAsync(new Car { Condition = CarCondition.New });

            // Assert
            result.ShouldReport("RegistrationPlate", "RegistrationPlate is required.");
        }

        [Fact]
        public void Otherwise_OnABlockNoWhenReturned_Throws()
        {
            // Arrange
            var block = default(ConditionBlock<Car>);

            // Act
            var act = () => block.Otherwise(() => { });

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("*When or Unless*");
        }

        [Fact]
        public async Task When_AfterTheFirstValidation_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty();

            await validator.ValidateAsync(new Car());

            // Act
            var act = () => validator.When(_ => true, () => { });

            // Assert
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public async Task Otherwise_AfterTheFirstValidation_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            var block = validator.When(_ => true, () => validator.Property(c => c.Vin).NotEmpty());

            await validator.ValidateAsync(new Car());

            // Act
            var act = () => block.Otherwise(() => { });

            // Assert
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void When_WithANullCondition_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();

            // Act
            var act = () => validator.When(null!, () => { });

            // Assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("condition");
        }

        [Fact]
        public void When_WithANullDelegate_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();

            // Act
            var act = () => validator.When(_ => true, null!);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("declareRules");
        }

        [Fact]
        public async Task When_WithDataTheRunCarries_RunsEveryChainDeclaredInside()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.When<ListingPolicy>((_, policy) => policy.RequiresServiceHistory, () =>
            {
                validator.Property(c => c.ServiceHistory).NotEmpty();
                validator.Property(c => c.ServiceIntervalKm).NotNull();
            });

            var options = new NValidationOptions { ValidationData = [new ListingPolicy(200_000, RequiresServiceHistory: true)] };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.ShouldReport([
                new("ServiceHistory", "ServiceHistory is required."),
                new("ServiceIntervalKm", "ServiceIntervalKm is required.")]);
        }

        [Fact]
        public async Task When_WithDataTheRunDoesNotCarry_SkipsEveryChainDeclaredInside()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.When<ListingPolicy>((_, _) => true, () => validator.Property(c => c.ServiceHistory).NotEmpty());

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task When_WithAPlainBlockInsideADataBlock_NeedsBoth()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.When<ListingPolicy>((_, _) => true, () =>
                validator.When(c => c.Condition == CarCondition.Used, () => validator.Property(c => c.IntakeCondition).NotNull()));

            var options = new NValidationOptions { ValidationData = [new ListingPolicy(200_000, false)] };

            // Act
            var withoutTheData = await validator.ValidateAsync(new Car { Condition = CarCondition.Used });
            var withTheData = await validator.ValidateAsync(new Car { Condition = CarCondition.Used }, options);

            // Assert
            withoutTheData.Errors.Should().BeEmpty();
            withTheData.ShouldReport("IntakeCondition", "IntakeCondition is required.");
        }

        [Fact]
        public async Task When_OnTheElementBuilder_NarrowsTheEntriesItsConditionHoldsFor()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory).ForEach(record =>
                record.When(r => r.Cost > 0m, () => record.Property(r => r.Workshop).NotEmpty()));

            var car = new Car
            {
                ServiceHistory = [new ServiceRecord { Cost = 0m }, new ServiceRecord { Cost = 120m }],
            };

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("ServiceHistory[1].Workshop", "Workshop is required.");
        }

        [Fact]
        public async Task Otherwise_OnTheElementBuilder_RunsForTheOtherEntries()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory).ForEach(record =>
                record
                    .When(r => r.Cost > 0m, () => record.Property(r => r.Workshop).NotEmpty())
                    .Otherwise(() => record.Property(r => r.Mileage).GreaterThan(0)));

            var car = new Car
            {
                ServiceHistory = [new ServiceRecord { Cost = 0m, Workshop = "Aurora" }, new ServiceRecord { Cost = 120m, Workshop = "Northgate" }],
            };

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("ServiceHistory[0].Mileage", "Mileage must be greater than 0.");
        }

        [Fact]
        public async Task When_WithDataOnTheElementBuilder_ReadsTheDataOfTheRun()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory).ForEach(record =>
                record.When<ListingPolicy>((_, policy) => policy.RequiresServiceHistory, () => record.Property(r => r.Workshop).NotEmpty()));

            var car = new Car { ServiceHistory = [new ServiceRecord()] };
            var options = new NValidationOptions { ValidationData = [new ListingPolicy(200_000, RequiresServiceHistory: true)] };

            // Act
            var withoutTheData = await validator.ValidateAsync(car);
            var withTheData = await validator.ValidateAsync(car, options);

            // Assert
            withoutTheData.Errors.Should().BeEmpty();
            withTheData.ShouldReport("ServiceHistory[0].Workshop", "Workshop is required.");
        }

        /// <summary>
        /// Declares its chain with a reachability predicate, which only the base class offers, inside a
        /// block whose condition reaches through the same object.
        /// </summary>
        private sealed class ConditionedNamedNestedPropertyCarValidator : Validator<Car>
        {
            public ConditionedNamedNestedPropertyCarValidator()
            {
                this.When(c => c.Model!.Name != "Aurora", () =>
                    this.Property("Model.Name", static c => c.Model!.Name, static c => c.Model != null).NotEmpty());
            }
        }
    }
}
