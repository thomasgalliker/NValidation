using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace NValidation.AspNetCore.Tests
{
    /// <summary>
    /// The filter through a real request pipeline: what a client actually receives when a payload is
    /// invalid, when an endpoint validates itself, and when nothing validates a payload at all.
    /// </summary>
    [Collection(Collections.SampleApi)]
    [Trait(Traits.Category, Traits.IntegrationTests)]
    public class ValidationActionFilterIntegrationTests
    {
        private readonly SampleApiTestFixture fixture;

        public ValidationActionFilterIntegrationTests(SampleApiTestFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task Create_WithAValidCar_RunsTheAction()
        {
            // Arrange
            var car = CreateValidCar();
            var httpClient = this.fixture.GetHttpClient();

            // Act
            var response = await httpClient.PostAsJsonAsync("api/cars", car);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        /// <summary>
        /// The action holds no validation code, so a 400 here is the filter's doing.
        /// </summary>
        [Fact]
        public async Task Create_WithAnInvalidCar_ReportsProblemDetails()
        {
            // Arrange
            var httpClient = this.fixture.GetHttpClient();
            var car = CreateValidCar();
            car["vin"] = "TOO-SHORT";

            // Act
            var response = await httpClient.PostAsJsonAsync("api/cars", car);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

            var errors = await GetErrorsAsync(response);
            errors.Should().ContainKey("Vin");
        }

        /// <summary>
        /// A nested payload is validated by its own validator, and its property names carry the path to it, so a
        /// form can bind each message to the input it belongs to.
        /// </summary>
        [Fact]
        public async Task Create_WithAnInvalidManufacturer_PrefixesTheErrorCodes()
        {
            // Arrange
            var httpClient = this.fixture.GetHttpClient();
            var car = CreateValidCar();
            car["model"] = new Dictionary<string, object?>
            {
                ["name"] = "Golf",
                ["manufacturer"] = new Dictionary<string, object?>
                {
                    ["name"] = "",
                    ["countryCode"] = "DE",
                    ["contactEmail"] = "not-an-email",
                },
                ["seatCount"] = 5,
                ["basePrice"] = 32000,
            };

            // Act
            var response = await httpClient.PostAsJsonAsync("api/cars", car);

            // Assert
            var errors = await GetErrorsAsync(response);
            errors.Should().ContainKeys(
                "Model.Manufacturer.Name",
                "Model.Manufacturer.CountryCode",
                "Model.Manufacturer.ContactEmail");
        }

        /// <summary>
        /// Every failure of one request is reported together, rather than one per round trip.
        /// </summary>
        [Fact]
        public async Task Create_WithSeveralInvalidProperties_ReportsThemTogether()
        {
            // Arrange
            var httpClient = this.fixture.GetHttpClient();
            var car = CreateValidCar();
            car["vin"] = "TOO-SHORT";
            car["mileage"] = -1;

            // Act
            var response = await httpClient.PostAsJsonAsync("api/cars", car);

            // Assert
            var errors = await GetErrorsAsync(response);
            errors.Should().ContainKeys("Vin", "Mileage");
        }

        /// <summary>
        /// The car id is where the request was addressed, not what it carried, so only the body is
        /// validated.
        /// </summary>
        [Fact]
        public async Task Update_ValidatesTheBodyAndNotTheRoute()
        {
            // Arrange
            var httpClient = this.fixture.GetHttpClient();
            var car = CreateValidCar();
            car["mileage"] = -1;

            // Act
            var response = await httpClient.PutAsJsonAsync("api/cars/7", car);

            // Assert
            var errors = await GetErrorsAsync(response);
            errors.Should().ContainSingle().Which.Key.Should().Be("Mileage");
        }

        /// <summary>
        /// The excluded endpoint runs the very same validator itself, so the rules stay in one place
        /// while the response keeps the shape its clients parse.
        /// </summary>
        [Fact]
        public async Task Valuate_WithAnInvalidCar_KeepsItsOwnErrorShape()
        {
            // Arrange
            var httpClient = this.fixture.GetHttpClient();
            var valuation = new Dictionary<string, object?> { ["vin"] = "TOO-SHORT", ["mileage"] = 10 };

            // Act
            var response = await httpClient.PostAsJsonAsync("api/cars/valuations", valuation);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            body.RootElement.GetProperty("error").GetString().Should().Be("invalid_request");
            body.RootElement.TryGetProperty("errors", out _).Should().BeFalse("the endpoint does not answer in problem details");
        }

        [Fact]
        public async Task Valuate_WithAValidCar_RunsTheAction()
        {
            // Arrange
            var httpClient = this.fixture.GetHttpClient();
            var valuation = new Dictionary<string, object?> { ["vin"] = ValidVin, ["mileage"] = 10 };

            // Act
            var response = await httpClient.PostAsJsonAsync("api/cars/valuations", valuation);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Import_WithoutAValidator_AndIgnore_RunsTheAction()
        {
            // Arrange
            var httpClient = this.fixture.GetHttpClient();

            // Act
            var response = await httpClient.PostAsJsonAsync("api/cars/imports", CreateImport());

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        [Fact]
        public async Task Import_WithoutAValidator_AndLog_RunsTheAction()
        {
            // Arrange
            using var loggingFixture = new LoggingSampleApiTestFixture();
            var httpClient = loggingFixture.GetHttpClient();

            // Act
            var response = await httpClient.PostAsJsonAsync("api/cars/imports", CreateImport());

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        /// <summary>
        /// At the strictest setting a payload nobody validates is a fault of the application, not of the
        /// request — so it fails as one, loudly, where it can still be fixed.
        /// </summary>
        [Fact]
        public async Task Import_WithoutAValidator_AndThrow_FailsTheRequest()
        {
            // Arrange
            using var throwingFixture = new ThrowingSampleApiTestFixture();
            var httpClient = throwingFixture.GetHttpClient();

            // Act
            var response = await httpClient.PostAsJsonAsync("api/cars/imports", CreateImport());

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }

        /// <summary>
        /// The exclusion also silences the behaviour: what is marked as deliberately unvalidated was
        /// thought about, so even the strictest setting has nothing to report about it.
        /// </summary>
        [Fact]
        public async Task Valuate_WithThrow_IsUnaffectedByTheExclusion()
        {
            // Arrange
            using var throwingFixture = new ThrowingSampleApiTestFixture();
            var httpClient = throwingFixture.GetHttpClient();
            var valuation = new Dictionary<string, object?> { ["vin"] = ValidVin, ["mileage"] = 10 };

            // Act
            var response = await httpClient.PostAsJsonAsync("api/cars/valuations", valuation);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        /// <summary>
        /// The filter is an MVC filter, so a minimal API endpoint is untouched by it and keeps validating
        /// in its handler.
        /// </summary>
        [Fact]
        public async Task MinimalApi_WithAnInvalidCar_StillValidatesInTheHandler()
        {
            // Arrange
            var httpClient = this.fixture.GetHttpClient();
            var car = CreateValidCar();
            car["vin"] = "TOO-SHORT";

            // Act
            var response = await httpClient.PostAsJsonAsync("cars", car);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await GetErrorsAsync(response)).Should().ContainKey("Vin");
        }

        /// <summary>
        /// The Create endpoint selects the group the valuation chain is in; the update endpoint selects
        /// nothing, so the same body is accepted there.
        /// </summary>
        [Fact]
        public async Task Create_WithATradeInAboveThePurchasePrice_ReportsTheChainOfTheCreateGroup()
        {
            // Arrange
            var httpClient = this.fixture.GetHttpClient();
            var car = CreateValidCar();
            car["tradeInValue"] = 99_999;

            // Act
            var response = await httpClient.PostAsJsonAsync("api/cars", car);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await GetErrorsAsync(response)).Should().ContainKey("TradeInValue");
        }

        [Fact]
        public async Task Update_WithATradeInAboveThePurchasePrice_RunsTheAction()
        {
            // Arrange
            var httpClient = this.fixture.GetHttpClient();
            var car = CreateValidCar();
            car["tradeInValue"] = 99_999;

            // Act
            var response = await httpClient.PutAsJsonAsync("api/cars/7", car);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task MinimalApi_WithATradeInAboveThePurchasePrice_ReportsTheChainOfTheCreateGroup()
        {
            // Arrange
            var httpClient = this.fixture.GetHttpClient();
            var car = CreateValidCar();
            car["tradeInValue"] = 99_999;

            // Act
            var response = await httpClient.PostAsJsonAsync("cars", car);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await GetErrorsAsync(response)).Should().ContainKey("TradeInValue");
        }

        /// <summary>
        /// The listing check runs the Listing group alone: a car without a plate is refused there, and a
        /// VIN the rest of the API would refuse does not concern it.
        /// </summary>
        [Fact]
        public async Task CheckListing_WithoutARegistrationPlate_ReportsItAlone()
        {
            // Arrange
            var httpClient = this.fixture.GetHttpClient();
            var car = CreateValidCar();
            car["vin"] = "TOO-SHORT";

            // Act
            var response = await httpClient.PostAsJsonAsync("api/cars/listing-check", car);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await GetErrorsAsync(response)).Keys.Should().Equal("RegistrationPlate");
        }

        [Fact]
        public async Task CheckListing_WithARegistrationPlate_AcceptsACarTheRestWouldRefuse()
        {
            // Arrange
            var httpClient = this.fixture.GetHttpClient();
            var car = CreateValidCar();
            car["vin"] = "TOO-SHORT";
            car["registrationPlate"] = "ZH 100 200";

            // Act
            var response = await httpClient.PostAsJsonAsync("api/cars/listing-check", car);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        /// <summary>
        /// The minimal API listing check hands the validation the market's policy as well: a car driven
        /// further than the market lists, and without the history it asks for, is told about both.
        /// </summary>
        [Fact]
        public async Task MinimalApiListingCheck_WithACarBeyondThePolicy_ReportsWhatTheMarketAsksFor()
        {
            // Arrange
            var httpClient = this.fixture.GetHttpClient();
            var car = CreateValidCar();
            car["registrationPlate"] = "ZH 100 200";
            car["mileage"] = 250_000;

            // Act
            var response = await httpClient.PostAsJsonAsync("cars/listing-check", car);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await GetErrorsAsync(response)).Keys.Should().BeEquivalentTo("Mileage", "ServiceHistory");
        }

        [Fact]
        public async Task MinimalApiListingCheck_WithACarThePolicyAccepts_Succeeds()
        {
            // Arrange
            var httpClient = this.fixture.GetHttpClient();
            var car = CreateValidCar();
            car["registrationPlate"] = "ZH 100 200";
            car["serviceHistory"] = new[] { new Dictionary<string, object?> { ["workshop"] = "Aurora", ["mileage"] = 10_000, ["cost"] = 120 } };

            // Act
            var response = await httpClient.PostAsJsonAsync("cars/listing-check", car);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        private const string ValidVin = "WVWZZZ1JZXW000001";

        private static Dictionary<string, object?> CreateValidCar()
        {
            return new Dictionary<string, object?>
            {
                ["vin"] = ValidVin,
                ["model"] = new Dictionary<string, object?>
                {
                    ["name"] = "Golf",
                    ["manufacturer"] = new Dictionary<string, object?>
                    {
                        ["name"] = "Volkswagen",
                        ["countryCode"] = "DEU",
                        ["contactEmail"] = "info@volkswagen.example",
                    },
                    ["seatCount"] = 5,
                    ["basePrice"] = 32000,
                },
                ["mileage"] = 42000,
                ["purchasePrice"] = 18500,
                ["firstRegistration"] = "2019-03-01T00:00:00",
            };
        }

        private static Dictionary<string, object?> CreateImport()
        {
            return new Dictionary<string, object?> { ["sourceSystem"] = "legacy", ["payload"] = "{}" };
        }

        private static async Task<IDictionary<string, string[]>> GetErrorsAsync(HttpResponseMessage response)
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<JsonElement>();

            return problemDetails.GetProperty("errors").Deserialize<Dictionary<string, string[]>>()!;
        }
    }
}
