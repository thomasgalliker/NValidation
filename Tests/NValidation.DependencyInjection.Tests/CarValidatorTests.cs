namespace NValidation.DependencyInjection.Tests
{
    /// <summary>
    /// Whole-payload scenarios for <see cref="CarValidator"/>, the validator the sample API exposes.
    /// The rest of the suite takes one rule at a time; these take a car the way a client sends one —
    /// several things wrong at once, across a nested object and a collection — and assert the complete
    /// set of property names a caller would have to act on.
    /// </summary>
    /// <remarks>
    /// Resolved from a container configured like the sample's, so the dependency chain each scenario
    /// leans on (car → model → manufacturer, and car → service record) is the one that ships.
    /// </remarks>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class CarValidatorTests
    {
        private readonly IValidator<Car> validator;

        public CarValidatorTests()
        {
            this.validator = new ServiceCollection()
                .AddNValidation(o => o.AddValidatorsFromAssembly(typeof(CarValidator).Assembly))
                .BuildServiceProvider()
                .GetRequiredService<IValidator<Car>>();
        }

        [Fact]
        public async Task ValidateAsync_ReportsNothing_ForACarWhichIsEntirelyValid()
        {
            // Arrange
            var car = Cars.Car();

            // Act
            var result = await this.validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        /// <summary>
        /// The point of validating a whole payload rather than failing at the first problem: a client
        /// filling in a form is told everything it has to fix, in one response.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_ReportsEveryBrokenProperty_NotOnlyTheFirst()
        {
            // Arrange
            var car = Cars.Car();
            car.Vin = "TOO-SHORT";
            car.Mileage = -1;
            car.PurchasePrice = -1000m;
            car.SoldDate = car.FirstRegistration.AddDays(-1);
            car.FeatureIds = [1, 1];

            // Act
            var result = await this.validator.ValidateAsync(car);

            // Assert
            result.ShouldReport([
                new("Vin", "The VIN must be exactly 17 characters long."),
                new("Mileage", "Mileage must be greater than or equal to 0."),
                new("PurchasePrice", "PurchasePrice must be greater than 0."),
                new("SoldDate", "SoldDate must be greater than or equal to Registration date."),
                new("FeatureIds", "FeatureIds must not contain duplicate entries.")]);
        }

        /// <summary>
        /// A property reports one message even when it breaks several of its rules, so a caller binding
        /// per field is not handed a pile for one input.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_ReportsOneFailurePerProperty()
        {
            // Arrange
            var car = Cars.Car();

            // Empty breaks NotEmpty, and is also not 17 characters long.
            car.Vin = "";

            // Act
            var result = await this.validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("Vin", "Vin is required.");
        }

        /// <summary>
        /// A nested validator keeps its own flat property names and the parent prefixes them, so the path a
        /// failure reports is the path into the payload — however deep it was declared.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_ReportsANestedObjectsFailuresUnderItsPathIntoThePayload()
        {
            // Arrange
            var car = Cars.Car();
            car.Model!.Name = "";
            car.Model.Manufacturer!.CountryCode = "CH";

            // Act
            var result = await this.validator.ValidateAsync(car);

            // Assert
            result.ShouldReport([
                new("Model.Name", "Name is required."),
                new("Model.Manufacturer.CountryCode", "CountryCode must be exactly 3 characters long.")]);
        }

        /// <summary>
        /// The collection's entries are judged by the service record's own validator, and each failure
        /// names the row it came from.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_ReportsEachBrokenServiceRecordUnderItsPosition()
        {
            // Arrange
            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = "Aurora", Mileage = 10_000, Cost = 120m },
                new ServiceRecord { Workshop = null, Mileage = 20_000, Cost = 90m },
                new ServiceRecord { Workshop = "Northgate", Mileage = 30_000, Cost = 0m },
            ];

            // Act
            var result = await this.validator.ValidateAsync(car);

            // Assert
            result.ShouldReport([
                new("ServiceHistory[1].Workshop", "Workshop is required."),
                new("ServiceHistory[2].Cost", "Cost must be greater than 0.")]);
        }

        /// <summary>
        /// The problems a payload has across all three kinds of rule at once — its own properties, a
        /// nested object and a collection entry — arrive together.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_ReportsPropertyNestedAndElementFailuresTogether()
        {
            // Arrange
            var car = Cars.Car();
            car.FeatureIds = [1, 1];
            car.Model!.Manufacturer!.ContactEmail = "not-an-email";
            car.ServiceHistory = [new ServiceRecord { Workshop = null, Mileage = 10_000, Cost = 10m }];

            // Act
            var result = await this.validator.ValidateAsync(car);

            // Assert
            result.ShouldReport([
                new("FeatureIds", "FeatureIds must not contain duplicate entries."),
                new("Model.Manufacturer.ContactEmail", "ContactEmail is not a valid email address."),
                new("ServiceHistory[0].Workshop", "Workshop is required.")]);
        }

        /// <summary>
        /// A rule about the collection may consult the object it hangs off — here, that a car cannot
        /// have been serviced at a mileage it has never reached.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_ReportsAServiceRecordedBeyondTheCarsOwnMileage()
        {
            // Arrange
            var car = Cars.Car();
            car.Mileage = 42_000;
            car.ServiceHistory = [new ServiceRecord { Workshop = "Aurora", Mileage = 50_000, Cost = 120m }];

            // Act
            var result = await this.validator.ValidateAsync(car);

            // Assert
            result.ShouldReport(
                "ServiceHistory", "A service cannot be recorded at a higher mileage than the car has reached.");
        }

        /// <summary>
        /// The entries are not judged at all once the collection itself is wrong: too many rows is one
        /// thing to fix, and reporting it alongside a complaint about each row would bury it.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_DoesNotJudgeTheEntries_WhenThereAreTooManyOfThem()
        {
            // Arrange
            var car = Cars.Car();
            car.ServiceHistory = Enumerable
                .Range(0, CarValidator.MaximumServiceRecords + 1)
                .Select(_ => new ServiceRecord { Workshop = null, Mileage = 100, Cost = 10m })
                .ToList();

            // Act
            var result = await this.validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("ServiceHistory", "ServiceHistory must not contain more than 20 entries.");
        }

        /// <summary>
        /// A listing check hears about what a listing needs and nothing else: a broken VIN is a matter for
        /// the car's own record, not for whether it can be offered. The price is both, so it is reported
        /// either way.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithOnlyTheListingGroup_ReportsWhatAListingNeedsAlone()
        {
            // Arrange
            var car = Cars.Car();
            car.Vin = "TOO-SHORT";
            car.PurchasePrice = 0m;
            car.RegistrationPlate = null;

            var options = new NValidationOptions { ValidationGroups = ValidationGroups.Only(CarValidator.ListingGroup) };

            // Act
            var result = await this.validator.ValidateAsync(car, options);

            // Assert
            result.ShouldReport([
                new("PurchasePrice", "PurchasePrice must be greater than 0."),
                new("RegistrationPlate", "RegistrationPlate is required.")]);
        }

        /// <summary>
        /// What a market asks of a listed car is handed over by the caller: a car driven further than the
        /// market lists, and without the history it requires, is told about both.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithOnlyTheListingGroupAndAPolicy_ReportsWhatTheMarketAsksFor()
        {
            // Arrange
            var car = Cars.Car();
            car.Mileage = 250_000;
            car.ServiceHistory = null;

            var options = new NValidationOptions
            {
                ValidationGroups = ValidationGroups.Only(CarValidator.ListingGroup),
                ValidationData = [new ListingPolicy(MaximumMileage: 200_000, RequiresServiceHistory: true)],
            };

            // Act
            var result = await this.validator.ValidateAsync(car, options);

            // Assert
            result.ShouldReport([
                new("Mileage", "The mileage is above what this market lists."),
                new("ServiceHistory", "ServiceHistory is required.")]);
        }

        [Fact]
        public async Task ValidateAsync_WithOnlyTheListingGroupButNoPolicy_LeavesWhatAMarketWouldDecideAlone()
        {
            // Arrange
            var car = Cars.Car();
            car.Mileage = 250_000;
            car.ServiceHistory = null;

            var options = new NValidationOptions { ValidationGroups = ValidationGroups.Only(CarValidator.ListingGroup) };

            // Act
            var result = await this.validator.ValidateAsync(car, options);

            // Assert
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateAsync_WithOnlyTheListingGroup_AsksAUsedCarWhatItArrivedInAndItsServiceInterval()
        {
            // Arrange
            var car = Cars.Car();
            car.Condition = CarCondition.Used;
            car.IntakeCondition = null;
            car.ServiceIntervalKm = null;

            var options = new NValidationOptions { ValidationGroups = ValidationGroups.Only(CarValidator.ListingGroup) };

            // Act
            var result = await this.validator.ValidateAsync(car, options);

            // Assert
            result.ShouldReport([
                new("IntakeCondition", "IntakeCondition is required."),
                new("ServiceIntervalKm", "ServiceIntervalKm is required.")]);
        }

        [Fact]
        public async Task ValidateAsync_WithOnlyTheListingGroup_LeavesANewCarWithoutAnIntakeConditionAlone()
        {
            // Arrange
            var car = Cars.Car();
            car.Condition = CarCondition.New;
            car.IntakeCondition = null;
            car.ServiceIntervalKm = null;

            var options = new NValidationOptions { ValidationGroups = ValidationGroups.Only(CarValidator.ListingGroup) };

            // Act
            var result = await this.validator.ValidateAsync(car, options);

            // Assert
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateAsync_WithoutASelection_LeavesTheListingChecksAlone()
        {
            // Arrange
            var car = Cars.Car();
            car.RegistrationPlate = null;

            // Act
            var result = await this.validator.ValidateAsync(car);

            // Assert
            result.Errors.Should().BeEmpty();
        }

        /// <summary>
        /// A history which is entirely in order is silent — the new rules do not report on a car that
        /// has nothing wrong with it.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_ReportsNothing_ForACarWithAValidServiceHistory()
        {
            // Arrange
            var car = Cars.Car();
            car.ServiceHistory =
            [
                new ServiceRecord { Workshop = "Aurora", Mileage = 10_000, Cost = 120m },
                new ServiceRecord { Workshop = "Northgate", Mileage = 30_000, Cost = 80m },
            ];

            // Act
            var result = await this.validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }
    }
}
