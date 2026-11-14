namespace NValidation.Tests
{
    public partial class PropertyRuleBuilderTests
    {
        [Fact]
        public async Task When_WithDataThatSatisfiesTheCondition_RunsTheChain()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .NotEmpty()
                .When<ListingPolicy>((_, policy) => policy.RequiresServiceHistory);

            var options = new NValidationOptions { ValidationData = [new ListingPolicy(200_000, RequiresServiceHistory: true)] };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.ShouldReport("ServiceHistory", "ServiceHistory is required.");
        }

        [Fact]
        public async Task When_WithDataThatFailsTheCondition_SkipsTheChain()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .NotEmpty()
                .When<ListingPolicy>((_, policy) => policy.RequiresServiceHistory);

            var options = new NValidationOptions { ValidationData = [new ListingPolicy(200_000, RequiresServiceHistory: false)] };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.Errors.Should().BeEmpty();
        }

        /// <summary>
        /// A caller that hands no policy over did not ask for what the policy would decide, so the chain
        /// is passed by — without reading the property, as any chain whose condition does not hold.
        /// </summary>
        [Fact]
        public async Task When_WithDataTheRunDoesNotCarry_SkipsTheChainWithoutReadingTheProperty()
        {
            // Arrange
            var validator = new TestValidator<CountingPayload>();
            validator.Property(p => p.Name).NotEmpty().When<ListingPolicy>((_, _) => true);

            var payload = new CountingPayload();

            // Act
            var result = await validator.ValidateAsync(payload);

            // Assert
            result.Errors.Should().BeEmpty();
            payload.Reads.Should().Be(0);
        }

        [Fact]
        public async Task When_WithDataOfAnotherType_SkipsTheChain()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty().When<ListingPolicy>((_, _) => true);

            var options = new NValidationOptions { ValidationData = ["not a policy"] };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task When_WithDataAfterAPlainCondition_IsNotAskedWhereThePlainOneFails()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin)
                .NotEmpty()
                .When(_ => false)
                .When<ListingPolicy>((_, _) => throw new InvalidOperationException("asked out of order"));

            var options = new NValidationOptions { ValidationData = [new ListingPolicy(200_000, false)] };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task When_WithDataBeforeAPlainCondition_KeepsThePlainOneBehindIt()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin)
                .NotEmpty()
                .When<ListingPolicy>((_, _) => false)
                .When(_ => throw new InvalidOperationException("asked out of order"));

            var options = new NValidationOptions { ValidationData = [new ListingPolicy(200_000, false)] };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.Errors.Should().BeEmpty();
        }

        /// <summary>
        /// The guard of a path through an object the payload omitted is asked first, so a condition which
        /// reaches through the same object never meets it absent.
        /// </summary>
        [Fact]
        public async Task When_WithData_IsAskedAfterTheReachabilityGuard()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Model!.Name)
                .NotEmpty()
                .When<ListingPolicy>((car, _) => car.Model!.Name != "Aurora");

            var options = new NValidationOptions { ValidationData = [new ListingPolicy(200_000, false)] };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task When_WithDataOnAnElementChain_ReadsTheDataOfTheRun()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .ForEach(record => record.Property(r => r.Workshop)
                    .NotEmpty()
                    .When<ListingPolicy>((_, policy) => policy.RequiresServiceHistory));

            var car = new Car { ServiceHistory = [new ServiceRecord()] };

            var options = new NValidationOptions { ValidationData = [new ListingPolicy(200_000, RequiresServiceHistory: true)] };

            // Act
            var result = await validator.ValidateAsync(car, options);

            // Assert
            result.ShouldReport("ServiceHistory[0].Workshop", "Workshop is required.");
        }

        [Fact]
        public void When_WithANullDataCondition_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();

            // Act
            var act = () => validator.Property(c => c.Vin).NotEmpty().When<ListingPolicy>(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("condition");
        }

        [Fact]
        public async Task When_WithDataAfterTheFirstValidation_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            var chain = validator.Property(c => c.Vin).NotEmpty();

            await validator.ValidateAsync(new Car());

            // Act
            var act = () => chain.When<ListingPolicy>((_, _) => true);

            // Assert
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public async Task Must_WithDataThePredicateRejects_ReportsTheValue()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Mileage).Must<ListingPolicy>((_, mileage, policy) => mileage <= policy.MaximumMileage);

            var car = new Car { Mileage = 250_000 };
            var options = new NValidationOptions { ValidationData = [new ListingPolicy(200_000, false)] };

            // Act
            var result = await validator.ValidateAsync(car, options);

            // Assert
            result.ShouldReport("Mileage", "Mileage is not valid.");
        }

        [Fact]
        public async Task Must_WithDataThePredicateRejects_ReportsTheMustErrorCode()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Mileage).Must<ListingPolicy>((_, mileage, policy) => mileage <= policy.MaximumMileage);

            var car = new Car { Mileage = 250_000 };
            var options = new NValidationOptions { ValidationData = [new ListingPolicy(200_000, false)] };

            // Act
            var result = await validator.ValidateAsync(car, options);

            // Assert
            result.ShouldReportErrorCode("Mileage", "Must");
        }

        [Theory]
        [InlineData(199_999)]
        [InlineData(200_000)]
        public async Task Must_WithDataThePredicateAccepts_Passes(int mileage)
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Mileage).Must<ListingPolicy>((_, value, policy) => value <= policy.MaximumMileage);

            var car = new Car { Mileage = mileage };
            var options = new NValidationOptions { ValidationData = [new ListingPolicy(200_000, false)] };

            // Act
            var result = await validator.ValidateAsync(car, options);

            // Assert
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task Must_WithDataTheRunDoesNotCarry_Passes()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Mileage).Must<ListingPolicy>((_, _, _) => false);

            // Act
            var result = await validator.ValidateAsync(new Car { Mileage = 250_000 });

            // Assert
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task Must_WithDataAndAnErrorCodeOfItsOwn_ReportsThatCode()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Mileage)
                .Must<ListingPolicy>((_, mileage, policy) => mileage <= policy.MaximumMileage)
                .WithErrorCode("ListingMileage");

            var car = new Car { Mileage = 250_000 };
            var options = new NValidationOptions { ValidationData = [new ListingPolicy(200_000, false)] };

            // Act
            var result = await validator.ValidateAsync(car, options);

            // Assert
            result.ShouldReportErrorCode("Mileage", "ListingMileage");
        }

        [Fact]
        public void Must_WithANullDataPredicate_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();

            // Act
            var act = () => validator.Property(c => c.Mileage).Must<ListingPolicy>(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("predicate");
        }

        [Fact]
        public async Task TryGetData_InARuleOfItsOwn_ReadsTheDataOfTheRun()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Mileage).Add(context =>
            {
                if (context.TryGetData<ListingPolicy>(out var policy) && context.Value > policy.MaximumMileage)
                {
                    context.AddError("ListingMileage", ("MaximumMileage", policy.MaximumMileage));
                }
            });

            var car = new Car { Mileage = 250_000 };
            var options = new NValidationOptions { ValidationData = [new ListingPolicy(200_000, false)] };

            // Act
            var result = await validator.ValidateAsync(car, options);

            // Assert
            result.ShouldReportErrorCode("Mileage", "ListingMileage");
        }

        [Fact]
        public async Task TryGetData_InAnAwaitingRule_ReadsTheDataOfTheRun()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Mileage).AddAsync(async (context, _) =>
            {
                await Task.Yield();

                if (context.TryGetData<ListingPolicy>(out var policy) && context.Value > policy.MaximumMileage)
                {
                    context.AddError("ListingMileage");
                }
            });

            var car = new Car { Mileage = 250_000 };
            var options = new NValidationOptions { ValidationData = [new ListingPolicy(200_000, false)] };

            // Act
            var result = await validator.ValidateAsync(car, options);

            // Assert
            result.ShouldReportErrorCode("Mileage", "ListingMileage");
        }

        [Fact]
        public async Task TryGetData_WithoutData_ReturnsFalse()
        {
            // Arrange
            bool? found = null;

            var validator = new TestValidator<Car>();
            validator.Property(c => c.Mileage).Add(context => found = context.TryGetData<ListingPolicy>(out _));

            // Act
            await validator.ValidateAsync(new Car());

            // Assert
            found.Should().BeFalse();
        }
    }
}
