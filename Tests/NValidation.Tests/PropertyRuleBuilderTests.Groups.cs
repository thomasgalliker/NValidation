namespace NValidation.Tests
{
    public partial class PropertyRuleBuilderTests
    {
        [Fact]
        public async Task WithGroup_WhenTheGroupIsSelected_RunsTheChain()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty().WithGroup("Create");

            var options = new NValidationOptions { ValidationGroups = "Create" };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.ShouldReport("Vin", "Vin is required.");
        }

        [Fact]
        public async Task WithGroup_WhenNoGroupIsSelected_SkipsTheChain()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty().WithGroup("Create");

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task WithGroup_WhenAnotherGroupIsSelected_SkipsTheChain()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty().WithGroup("Create");

            var options = new NValidationOptions { ValidationGroups = "Update" };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task WithGroup_WhenAllGroupsAreSelected_RunsTheChain()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty().WithGroup("Create");

            var options = new NValidationOptions { ValidationGroups = ValidationGroups.All };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.ShouldReport("Vin", "Vin is required.");
        }

        /// <summary>
        /// A chain in no group is not something a selection can switch off, which is what lets one chain
        /// be grouped without every other call site having to say so.
        /// </summary>
        [Fact]
        public async Task WithGroup_WhenTheGroupIsSelected_StillRunsTheChainsInNoGroup()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty().WithGroup("Create");
            validator.Property(c => c.RegistrationPlate).NotEmpty();

            var options = new NValidationOptions { ValidationGroups = "Create" };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.ShouldReport([
                new("Vin", "Vin is required."),
                new("RegistrationPlate", "RegistrationPlate is required.")]);
        }

        [Fact]
        public async Task WithGroup_WhenNoGroupIsSelected_StillRunsTheChainsInNoGroup()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty().WithGroup("Create");
            validator.Property(c => c.RegistrationPlate).NotEmpty();

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.ShouldReport("RegistrationPlate", "RegistrationPlate is required.");
        }

        [Fact]
        public async Task WithGroup_NamingTheDefaultGroupBesideAnother_RunsTheChainWithoutASelection()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty().WithGroup(ValidationGroups.DefaultGroup, "Listing");

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.ShouldReport("Vin", "Vin is required.");
        }

        [Fact]
        public async Task WithGroup_NamingTheDefaultGroupBesideAnother_RunsTheChainUnderOnlyThatGroup()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty().WithGroup(ValidationGroups.DefaultGroup, "Listing");
            validator.Property(c => c.RegistrationPlate).NotEmpty();

            var options = new NValidationOptions { ValidationGroups = ValidationGroups.Only("Listing") };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.ShouldReport("Vin", "Vin is required.");
        }

        [Fact]
        public async Task WithGroup_NamingTheDefaultGroupAlone_LeavesTheChainWhereItWas()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty().WithGroup(ValidationGroups.DefaultGroup);
            validator.Property(c => c.RegistrationPlate).NotEmpty().WithGroup("Listing");

            var options = new NValidationOptions { ValidationGroups = ValidationGroups.Only("Listing") };

            // Act
            var withoutASelection = await validator.ValidateAsync(new Car());
            var withOnlyTheGroup = await validator.ValidateAsync(new Car(), options);

            // Assert
            withoutASelection.ShouldReport("Vin", "Vin is required.");
            withOnlyTheGroup.ShouldReport("RegistrationPlate", "RegistrationPlate is required.");
        }

        /// <summary>
        /// The group covers the chain wherever it is written, exactly as a condition does.
        /// </summary>
        [Fact]
        public async Task WithGroup_GovernsEveryRuleOfTheChain()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin)
                .WithValidationBehavior(ValidationBehavior.All)
                .NotEmpty()
                .WithGroup("Create")
                .MaximumLength(3);

            var car = Cars.Car();
            car.Vin = "far too long";

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task WithGroup_CalledTwice_AddsBothGroups()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty().WithGroup("Create").WithGroup("Update");

            var options = new NValidationOptions { ValidationGroups = "Update" };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.ShouldReport("Vin", "Vin is required.");
        }

        [Fact]
        public async Task WithGroup_WithSeveralNames_AddsAllOfThem()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty().WithGroup("Create", "Update");

            var options = new NValidationOptions { ValidationGroups = "Update" };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.ShouldReport("Vin", "Vin is required.");
        }

        [Fact]
        public void WithGroup_WithoutAName_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();

            // Act
            var act = () => validator.Property(c => c.Vin).NotEmpty().WithGroup();

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("groups");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void WithGroup_WithABlankName_Throws(string? group)
        {
            // Arrange
            var validator = new TestValidator<Car>();

            // Act
            var act = () => validator.Property(c => c.Vin).NotEmpty().WithGroup(group!);

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("groups");
        }

        /// <summary>
        /// The group is asked before the condition, so a chain which does not apply costs nothing beyond
        /// the comparison that skipped it.
        /// </summary>
        [Fact]
        public async Task WithGroup_WhenTheGroupIsNotSelected_DoesNotAskTheCondition()
        {
            // Arrange
            var conditionWasAsked = false;

            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin)
                .NotEmpty()
                .When(_ =>
                {
                    conditionWasAsked = true;
                    return true;
                })
                .WithGroup("Create");

            // Act
            await validator.ValidateAsync(new Car());

            // Assert
            conditionWasAsked.Should().BeFalse();
        }

        [Fact]
        public async Task WithGroup_WhenTheGroupIsNotSelected_DoesNotReadTheProperty()
        {
            // Arrange
            var validator = new TestValidator<CountingPayload>();
            validator.Property(p => p.Name).NotEmpty().WithGroup("Create");

            var payload = new CountingPayload();

            // Act
            await validator.ValidateAsync(payload);

            // Assert
            payload.Reads.Should().Be(0);
        }

        /// <summary>
        /// A skipped chain reported nothing, so a run which stops at the first error has nothing to stop
        /// on and goes on to the next property.
        /// </summary>
        [Fact]
        public async Task WithGroup_AChainItSkipped_DoesNotStopARunThatStopsAtTheFirstError()
        {
            // Arrange
            var validator = new TestValidator<Car>
            {
                ValidationBehaviors = new() { Class = ValidationBehavior.StopAtFirstError },
            };
            validator.Property(c => c.Vin).NotEmpty().WithGroup("Create");
            validator.Property(c => c.RegistrationPlate).NotEmpty();

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.ShouldReport("RegistrationPlate", "RegistrationPlate is required.");
        }

        [Fact]
        public async Task WithGroup_OnAnAwaitingChain_SkipsItWhenTheGroupIsNotSelected()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin)
                .MustAsync(async (vin, _) =>
                {
                    await Task.Yield();
                    return vin != null;
                })
                .WithGroup("Create");

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task WithGroup_OnAnAwaitingChain_RunsItWhenTheGroupIsSelected()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin)
                .MustAsync(async (vin, _) =>
                {
                    await Task.Yield();
                    return vin != null;
                })
                .WithGroup("Create");

            var options = new NValidationOptions { ValidationGroups = "Create" };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.ShouldReportErrorCode("Vin", "Must");
        }

        /// <summary>
        /// Declared for this test alone, because the compiled accessor is cached per payload type and
        /// property name.
        /// </summary>
        private sealed class CountingPayload
        {
            public int Reads { get; private set; }

            public string? Name
            {
                get
                {
                    this.Reads++;

                    return null;
                }
            }
        }
    }
}
