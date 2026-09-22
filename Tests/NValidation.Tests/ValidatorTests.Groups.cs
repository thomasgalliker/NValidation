using NValidation.Internals;

namespace NValidation.Tests
{
    public partial class ValidatorTests
    {
        [Fact]
        public async Task Group_PutsEveryChainDeclaredInside_InTheGroup()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Group("Create", () =>
            {
                validator.Property(c => c.Vin).NotEmpty();
                validator.Property(c => c.RegistrationPlate).NotEmpty();
            });

            var options = new NValidationOptions { ValidationGroups = "Create" };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.ShouldReport([
                new("Vin", "Vin is required."),
                new("RegistrationPlate", "RegistrationPlate is required.")]);
        }

        [Fact]
        public async Task Group_WhenTheGroupIsNotSelected_SkipsEveryChainDeclaredInside()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Group("Create", () =>
            {
                validator.Property(c => c.Vin).NotEmpty();
                validator.Property(c => c.RegistrationPlate).NotEmpty();
            });

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task Group_LeavesAChainDeclaredAfterTheBlock_InNoGroup()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Group("Create", () => validator.Property(c => c.Vin).NotEmpty());
            validator.Property(c => c.RegistrationPlate).NotEmpty();

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.ShouldReport("RegistrationPlate", "RegistrationPlate is required.");
        }

        [Fact]
        public async Task Group_WithSeveralNames_PutsTheChainsInAllOfThem()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Group(["Create", "Update"], () => validator.Property(c => c.Vin).NotEmpty());

            var options = new NValidationOptions { ValidationGroups = "Update" };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.ShouldReport("Vin", "Vin is required.");
        }

        [Theory]
        [InlineData("Create")]
        [InlineData("Update")]
        public async Task Group_Nested_PutsTheChainInTheGroupsOfBothBlocks(string selectedGroup)
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Group("Create", () =>
                validator.Group("Update", () => validator.Property(c => c.Vin).NotEmpty()));

            var options = new NValidationOptions { ValidationGroups = selectedGroup };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.ShouldReport("Vin", "Vin is required.");
        }

        /// <summary>
        /// The inner block adds to the outer one for its own chains and takes nothing away from what the
        /// outer block goes on declaring.
        /// </summary>
        [Fact]
        public async Task Group_Nested_LeavesTheOuterGroupInPlaceAfterTheInnerBlock()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Group("Create", () =>
            {
                validator.Group("Update", () => validator.Property(c => c.Vin).NotEmpty());
                validator.Property(c => c.RegistrationPlate).NotEmpty();
            });

            var options = new NValidationOptions { ValidationGroups = "Update" };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.ShouldReport("Vin", "Vin is required.");
        }

        [Fact]
        public async Task Group_WithAChainNamingAGroupOfItsOwn_PutsItInBoth()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Group("Create", () => validator.Property(c => c.Vin).NotEmpty().WithGroup("Update"));

            var options = new NValidationOptions { ValidationGroups = "Update" };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.ShouldReport("Vin", "Vin is required.");
        }

        [Fact]
        public async Task Group_WhenTheBlockThrows_LeavesALaterChainInNoGroup()
        {
            // Arrange
            var validator = new TestValidator<Car>();

            var act = () => validator.Group("Create", () => throw new InvalidOperationException("declaration failed"));
            act.Should().Throw<InvalidOperationException>().WithMessage("declaration failed");

            validator.Property(c => c.RegistrationPlate).NotEmpty();

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.ShouldReport("RegistrationPlate", "RegistrationPlate is required.");
        }

        [Fact]
        public void Group_WithANullDelegate_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();

            // Act
            var act = () => validator.Group("Create", null!);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("declareRules");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Group_WithABlankName_Throws(string? group)
        {
            // Arrange
            var validator = new TestValidator<Car>();

            // Act
            var act = () => validator.Group(group!, () => validator.Property(c => c.Vin).NotEmpty());

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("group");
        }

        [Fact]
        public void Group_WithoutAName_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();

            // Act
            var act = () => validator.Group([], () => validator.Property(c => c.Vin).NotEmpty());

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("groups");
        }

        [Fact]
        public async Task Group_AfterTheFirstValidation_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty();

            await validator.ValidateAsync(Cars.Car());

            // Act
            var act = () => validator.Group("Create", () => validator.Property(c => c.Mileage).GreaterThan(0));

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("*already validated something*");
        }

        [Fact]
        public async Task Group_AppliesToAChainDeclaredByName()
        {
            // Arrange
            var validator = new GroupedNamedPropertyCarValidator();

            var options = new NValidationOptions { ValidationGroups = "Create" };

            // Act
            var withoutTheGroup = await validator.ValidateAsync(new Car());
            var withTheGroup = await validator.ValidateAsync(new Car(), options);

            // Assert
            withoutTheGroup.Errors.Should().BeEmpty();
            withTheGroup.ShouldReport("Vin", "Vin is required.");
        }

        [Fact]
        public async Task Group_AppliesToAChainDeclaredWithAReachabilityPredicate()
        {
            // Arrange
            var validator = new GroupedNamedNestedPropertyCarValidator();

            var options = new NValidationOptions { ValidationGroups = "Create" };

            // Act
            var withoutTheGroup = await validator.ValidateAsync(new Car { Model = new CarModel() });
            var withTheGroup = await validator.ValidateAsync(new Car { Model = new CarModel() }, options);

            // Assert
            withoutTheGroup.Errors.Should().BeEmpty();
            withTheGroup.ShouldReport("Model.Name", "Model.Name is required.");
        }

        [Fact]
        public async Task Group_AppliesToAChainDeclaredForTheElementItself()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceMileages)
                .ForEach(mileage => mileage.Group("Create", () => mileage.Element().GreaterThan(0)));

            var car = Cars.Car();
            car.ServiceMileages = [-1];

            var options = new NValidationOptions { ValidationGroups = "Create" };

            // Act
            var withoutTheGroup = await validator.ValidateAsync(car);
            var withTheGroup = await validator.ValidateAsync(car, options);

            // Assert
            withoutTheGroup.Errors.Should().BeEmpty();
            withTheGroup.ShouldReportErrorCode("ServiceMileages[0]", "GreaterThan");
        }

        /// <summary>
        /// What the registration hands a validator it constructed is a rung of the ladder like any other,
        /// so a host can name the groups its validators run without every call site repeating them.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithGroupsHandedByTheRegistration_SelectsThem()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty().WithGroup("Create");

            ((IValidationRegistrationTarget)validator).Options =
                new NValidationOptions { ValidationGroups = "Create" };

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.ShouldReport("Vin", "Vin is required.");
        }

        [Fact]
        public async Task ValidateAsync_WithGroupsHandedByTheRegistration_AreOutrankedByTheOptionsOfTheCall()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty().WithGroup("Create");

            ((IValidationRegistrationTarget)validator).Options =
                new NValidationOptions { ValidationGroups = "Create" };

            var options = new NValidationOptions { ValidationGroups = "Update" };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateAndThrowAsync_WithSelectedGroups_ReportsTheGroupedChain()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty().WithGroup("Create");

            var options = new NValidationOptions { ValidationGroups = "Create" };

            // Act
            var act = () => validator.ValidateAndThrowAsync(new Car(), options).AsTask();

            // Assert
            var validationException = (await act.Should().ThrowAsync<ValidationException>()).Which;
            validationException.ShouldReport("Vin", "Vin is required.");
        }

        [Fact]
        public async Task ValidateAndThrowAsync_WithoutSelectedGroups_LeavesTheGroupedChainAlone()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty().WithGroup("Create");

            var options = new NValidationOptions();

            // Act
            var act = () => validator.ValidateAndThrowAsync(new Car(), options).AsTask();

            // Assert
            await act.Should().NotThrowAsync();
        }

        private sealed class GroupedNamedPropertyCarValidator : Validator<Car>
        {
            public GroupedNamedPropertyCarValidator()
            {
                this.Group("Create", () => this.Property("Vin", static c => c.Vin).NotEmpty());
            }
        }

        private sealed class GroupedNamedNestedPropertyCarValidator : Validator<Car>
        {
            public GroupedNamedNestedPropertyCarValidator()
            {
                this.Group("Create", () =>
                    this.Property("Model.Name", static c => c.Model!.Name, static c => c.Model != null).NotEmpty());
            }
        }
    }
}
