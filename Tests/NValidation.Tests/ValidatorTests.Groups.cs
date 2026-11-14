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

        [Fact]
        public async Task ValidateAsync_WithOnly_SkipsTheChainsInTheDefaultGroup()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty();
            validator.Property(c => c.RegistrationPlate).NotEmpty().WithGroup("Listing");

            var options = new NValidationOptions { ValidationGroups = ValidationGroups.Only("Listing") };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.ShouldReport("RegistrationPlate", "RegistrationPlate is required.");
        }

        /// <summary>
        /// A selection which runs nothing of the validator it was passed to is a mistake — a mistyped name,
        /// the wrong validator — and passing would hide it, so the call refuses.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithOnlyAGroupTheValidatorDoesNotDeclare_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty().WithGroup("Create");
            validator.Property(c => c.RegistrationPlate).NotEmpty().WithGroup("Listing");

            var options = new NValidationOptions { ValidationGroups = ValidationGroups.Only("Listng") };

            // Act
            var act = () => validator.ValidateAsync(new Car(), options).AsTask();

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Only: Listng*Create, Listing*");
        }

        [Fact]
        public async Task ValidateAsync_WithOnlyOnAValidatorDeclaringNoGroup_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty();

            var options = new NValidationOptions { ValidationGroups = ValidationGroups.Only("Listing") };

            // Act
            var act = () => validator.ValidateAsync(new Car(), options).AsTask();

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*declares no group at all*");
        }

        [Fact]
        public async Task ValidateAsync_WithOnly_ReachesAGroupANestedValidatorDeclares()
        {
            // Arrange
            var modelValidator = new TestValidator<CarModel>();
            modelValidator.Property(m => m.Name).NotEmpty().WithGroup("Listing");
            modelValidator.Property(m => m.Manufacturer).NotNull();

            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty();
            validator.Property(c => c.Model).SetValidator(modelValidator);

            var options = new NValidationOptions { ValidationGroups = ValidationGroups.Only("Listing") };

            // Act
            var result = await validator.ValidateAsync(new Car { Model = new CarModel() }, options);

            // Assert
            result.ShouldReport("Model.Name", "Name is required.");
        }

        /// <summary>
        /// Reaching a group through a chain runs the part of the chain that hands the value on and nothing
        /// else: its own rules are in the default group, which the selection left out.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithOnly_DoesNotRunTheRulesOfTheChainItReachesThrough()
        {
            // Arrange
            var modelValidator = new TestValidator<CarModel>();
            modelValidator.Property(m => m.Name).NotEmpty().WithGroup("Listing");

            var validator = new TestValidator<Car>();
            validator.Property(c => c.Model).NotNull().SetValidator(modelValidator);

            var options = new NValidationOptions { ValidationGroups = ValidationGroups.Only("Listing") };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateAsync_WithOnly_DoesNotReachThroughAChainOutsideTheDefaultGroup()
        {
            // Arrange
            var modelValidator = new TestValidator<CarModel>();
            modelValidator.Property(m => m.Name).NotEmpty().WithGroup("Listing");

            var validator = new TestValidator<Car>();
            validator.Property(c => c.RegistrationPlate).NotEmpty().WithGroup("Listing");
            validator.Property(c => c.Model).SetValidator(modelValidator).WithGroup("Create");

            var options = new NValidationOptions { ValidationGroups = ValidationGroups.Only("Listing") };

            // Act
            var result = await validator.ValidateAsync(new Car { Model = new CarModel() }, options);

            // Assert
            result.ShouldReport("RegistrationPlate", "RegistrationPlate is required.");
        }

        /// <summary>
        /// A composed validator that has never heard of the selected groups cannot say which of its rules
        /// belong to them, so all of its default group is the answer.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithOnly_ValidatesANestedValidatorDeclaringNoneOfItsGroups_AsAPlainCallWould()
        {
            // Arrange
            var modelValidator = new TestValidator<CarModel>();
            modelValidator.Property(m => m.Name).NotEmpty();
            modelValidator.Property(m => m.Manufacturer).NotNull().WithGroup("Create");

            var validator = new TestValidator<Car>();
            validator.Property(c => c.Model).SetValidator(modelValidator).WithGroup("Listing");

            var options = new NValidationOptions { ValidationGroups = ValidationGroups.Only("Listing") };

            // Act
            var result = await validator.ValidateAsync(new Car { Model = new CarModel() }, options);

            // Assert
            result.ShouldReport("Model.Name", "Name is required.");
        }

        [Fact]
        public async Task ValidateAsync_WithOnly_RunsANestedValidatorDeclaringTheGroup_AsThatGroupAlone()
        {
            // Arrange
            var modelValidator = new TestValidator<CarModel>();
            modelValidator.Property(m => m.Name).NotEmpty();
            modelValidator.Property(m => m.BasePrice).NotNull().WithGroup("Listing");

            var validator = new TestValidator<Car>();
            validator.Property(c => c.Model).SetValidator(modelValidator).WithGroup("Listing");

            var options = new NValidationOptions { ValidationGroups = ValidationGroups.Only("Listing") };

            // Act
            var result = await validator.ValidateAsync(new Car { Model = new CarModel() }, options);

            // Assert
            result.ShouldReport("Model.BasePrice", "BasePrice is required.");
        }

        /// <summary>
        /// A nested validator keeps what it resolved for a selection its composer handed it twice in a row; a
        /// call selecting nothing still runs its default group rather than the selection it kept.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_AfterANestedValidatorKeptAnExclusiveSelection_RunsItsDefaultGroupForACallSelectingNothing()
        {
            // Arrange
            var modelValidator = new TestValidator<CarModel>();
            modelValidator.Property(m => m.Name).NotEmpty();
            modelValidator.Property(m => m.BasePrice).NotNull().WithGroup("Listing");

            var validator = new TestValidator<Car>();
            validator.Property(c => c.Model).SetValidator(modelValidator);

            var listingAlone = new NValidationOptions { ValidationGroups = ValidationGroups.Only("Listing") };
            var car = new Car { Model = new CarModel() };

            await validator.ValidateAsync(car, listingAlone);
            await validator.ValidateAsync(car, listingAlone);

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("Model.Name", "Name is required.");
        }

        [Fact]
        public async Task ValidateAsync_WithOnlyOnAChainComposingTwoValidators_RunsOnlyTheOneDeclaringTheGroup()
        {
            // Arrange
            var listingValidator = new TestValidator<CarModel>();
            listingValidator.Property(m => m.Name).NotEmpty().WithGroup("Listing");

            var otherValidator = new TestValidator<CarModel>();
            otherValidator.Property(m => m.Manufacturer).NotNull();

            var validator = new TestValidator<Car>();
            validator.Property(c => c.Model).SetValidator(listingValidator).SetValidator(otherValidator);

            var options = new NValidationOptions { ValidationGroups = ValidationGroups.Only("Listing") };

            // Act
            var result = await validator.ValidateAsync(new Car { Model = new CarModel() }, options);

            // Assert
            result.ShouldReport("Model.Name", "Name is required.");
        }

        [Fact]
        public async Task ValidateAsync_WithOnly_AChainItSkippedDoesNotStopARunThatStopsAtTheFirstError()
        {
            // Arrange
            var validator = new TestValidator<Car>
            {
                ValidationBehaviors = new() { Class = ValidationBehavior.StopAtFirstError },
            };
            validator.Property(c => c.Vin).NotEmpty();
            validator.Property(c => c.RegistrationPlate).NotEmpty().WithGroup("Listing");
            validator.Property(c => c.PreviousRegistrationPlate).NotEmpty().WithGroup("Listing");

            var options = new NValidationOptions { ValidationGroups = ValidationGroups.Only("Listing") };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.ShouldReport("RegistrationPlate", "RegistrationPlate is required.");
        }

        [Fact]
        public async Task ValidateAsync_WithOnlyOnAnAwaitingValidator_SkipsTheChainsInTheDefaultGroup()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).MustAsync((vin, _) => ValueTask.FromResult(vin != null));
            validator.Property(c => c.RegistrationPlate).NotEmpty().WithGroup("Listing");

            var options = new NValidationOptions { ValidationGroups = ValidationGroups.Only("Listing") };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.ShouldReport("RegistrationPlate", "RegistrationPlate is required.");
        }

        [Fact]
        public async Task ValidateAsync_WithOnly_ReachesAGroupAnAwaitingNestedValidatorDeclares()
        {
            // Arrange
            var modelValidator = new TestValidator<CarModel>();
            modelValidator.Property(m => m.Name)
                .MustAsync((name, _) => ValueTask.FromResult(name != null))
                .WithGroup("Listing");
            modelValidator.Property(m => m.Manufacturer).NotNull();

            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty();
            validator.Property(c => c.Model).NotNull().SetValidator(modelValidator);

            var options = new NValidationOptions { ValidationGroups = ValidationGroups.Only("Listing") };

            // Act
            var result = await validator.ValidateAsync(new Car { Model = new CarModel() }, options);

            // Assert
            result.ShouldReport("Model.Name", "Name is not valid.");
        }

        [Fact]
        public async Task ValidateAsync_WithOnly_ReachesAGroupTheEntriesOfAForEachDeclare()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty();
            validator.Property(c => c.ServiceHistory)
                .ForEach(record => record.Group("Listing", () => record.Property(r => r.Workshop).NotEmpty()));

            var car = new Car { ServiceHistory = [new ServiceRecord()] };

            var options = new NValidationOptions { ValidationGroups = ValidationGroups.Only("Listing") };

            // Act
            var result = await validator.ValidateAsync(car, options);

            // Assert
            result.ShouldReport("ServiceHistory[0].Workshop", "Workshop is required.");
        }

        /// <summary>
        /// The entries' own chains and their validator answer as one: where one of them declares a
        /// selected group, the other has nothing to contribute to that selection.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithOnly_PassesOverAnElementValidatorDeclaringNone_WhereTheEntriesOwnChainsDeclareOne()
        {
            // Arrange
            var recordValidator = new TestValidator<ServiceRecord>();
            recordValidator.Property(r => r.Cost).GreaterThan(0m);

            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory).ForEach(record =>
            {
                record.Group("Listing", () => record.Property(r => r.Workshop).NotEmpty());
                record.SetValidator(recordValidator);
            });

            var car = new Car { ServiceHistory = [new ServiceRecord()] };

            var options = new NValidationOptions { ValidationGroups = ValidationGroups.Only("Listing") };

            // Act
            var result = await validator.ValidateAsync(car, options);

            // Assert
            result.ShouldReport("ServiceHistory[0].Workshop", "Workshop is required.");
        }

        [Fact]
        public async Task ValidateAsync_WithOnly_ValidatesTheEntriesOfAForEachSelectedByName_AsAPlainCallWould()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .WithGroup("Listing")
                .ForEach(record => record.Property(r => r.Workshop).NotEmpty());

            var car = new Car { ServiceHistory = [new ServiceRecord()] };

            var options = new NValidationOptions { ValidationGroups = ValidationGroups.Only("Listing") };

            // Act
            var result = await validator.ValidateAsync(car, options);

            // Assert
            result.ShouldReport("ServiceHistory[0].Workshop", "Workshop is required.");
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
