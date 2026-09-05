namespace NValidation.Tests
{
    /// <summary>
    /// Whole-payload scenarios for <see cref="CarValidator"/>, the validator the sample API exposes.
    /// The rest of the suite takes one rule at a time; these take a car the way a client sends one —
    /// several things wrong at once, across a nested object and a collection — and assert the complete
    /// set of codes a caller would have to act on.
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
            car.SoldDate = car.FirstRegistration.AddDays(-1);
            car.FeatureIds = [1, 1];

            // Act
            var result = await this.validator.ValidateAsync(car);

            // Assert
            result.Errors.Select(error => error.Code).Should()
                .BeEquivalentTo(["Vin", "Mileage", "SoldDate", "FeatureIds"]);
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
            result.Errors.Should().ContainSingle().Which.Code.Should().Be("Vin");
        }

        /// <summary>
        /// A nested validator keeps its own flat codes and the parent prefixes them, so the path a
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
            result.Errors.Select(error => error.Code).Should()
                .BeEquivalentTo(["Model.Name", "Model.Manufacturer.CountryCode"]);
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
            result.Errors.Select(error => error.Code).Should()
                .BeEquivalentTo(["ServiceHistory[1].Workshop", "ServiceHistory[2].Cost"]);
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
            result.Errors.Select(error => error.Code).Should().BeEquivalentTo(
                ["FeatureIds", "Model.Manufacturer.ContactEmail", "ServiceHistory[0].Workshop"]);
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
            result.Errors.Should().ContainSingle().Which.Code.Should().Be("ServiceHistory");
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
            result.Errors.Should().ContainSingle().Which.Code.Should().Be("ServiceHistory");
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
