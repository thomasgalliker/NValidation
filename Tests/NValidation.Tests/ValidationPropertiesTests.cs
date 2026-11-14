using NValidation.Internals;

namespace NValidation.Tests
{
    /// <summary>
    /// What a selection of properties accepts and how it answers for a chain: in full where the chain is at
    /// or below a selected name, through to what the chain composes where a selected name lies below it,
    /// and not at all otherwise.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class ValidationPropertiesTests
    {
        [Fact]
        public void Constructor_WithNames_KeepsThem()
        {
            // Act
            var properties = new ValidationProperties("PurchasePrice", "Model.Name");

            // Assert
            properties.PropertyNames.Should().Equal("PurchasePrice", "Model.Name");
        }

        [Fact]
        public void Constructor_WithANameBelowAnotherNamedOne_KeepsTheOuterOne()
        {
            // Act
            var properties = new ValidationProperties("Model.Name", "Model");

            // Assert
            properties.PropertyNames.Should().Equal("Model");
        }

        [Fact]
        public void Constructor_WithoutAName_Throws()
        {
            // Act
            var act = () => new ValidationProperties();

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("propertyNames");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Constructor_WithABlankName_Throws(string? propertyName)
        {
            // Act
            var act = () => new ValidationProperties("Vin", propertyName!);

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("propertyNames");
        }

        [Theory]
        [InlineData("Model..Name")]
        [InlineData(".Name")]
        [InlineData("Model.")]
        public void Constructor_WithAnEmptySegment_Throws(string propertyName)
        {
            // Act
            var act = () => new ValidationProperties(propertyName);

            // Assert
            act.Should().Throw<ArgumentException>().WithMessage("*empty segment*");
        }

        /// <summary>
        /// A position would select one entry of a collection, which the selection does not do: a name
        /// without one already applies to every entry, and saying so is kinder than ignoring it.
        /// </summary>
        [Fact]
        public void Constructor_WithAPosition_Throws()
        {
            // Act
            var act = () => new ValidationProperties("ServiceHistory[0].Workshop");

            // Assert
            act.Should().Throw<ArgumentException>().WithMessage("*ServiceHistory.Workshop*");
        }

        [Fact]
        public void CollectionExpression_WithNames_SelectsThem()
        {
            // Act
            ValidationProperties properties = ["Vin", "Mileage"];

            // Assert
            properties.PropertyNames.Should().Equal("Vin", "Mileage");
        }

        [Fact]
        public void For_WithExpressions_NamesTheirPaths()
        {
            // Act
            var properties = ValidationProperties.For<Car>(c => c.PurchasePrice, c => c.Model!.Name);

            // Assert
            properties.PropertyNames.Should().Equal("PurchasePrice", "Model.Name");
        }

        [Fact]
        public void For_WithAnExpressionThatReachesNoProperty_Throws()
        {
            // Act
            var act = () => ValidationProperties.For<Car>(c => c.Vin!.ToUpperInvariant());

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ToString_JoinsTheNames()
        {
            // Act
            var text = new ValidationProperties("PurchasePrice", "Model.Name").ToString();

            // Assert
            text.Should().Be("PurchasePrice, Model.Name");
        }

        [Theory]
        [InlineData("Model.Name", "Run")]
        [InlineData("Model.Name.Length", "Run")]
        [InlineData("Model", "Composed")]
        [InlineData("Model.Manufacturer", "Skip")]
        [InlineData("Vin", "Skip")]
        [InlineData("model.name", "Run")]
        public void Match_ForAChain_SaysHowFarItRuns(string chainName, string expected)
        {
            // Arrange
            var properties = new ValidationProperties("Model.Name");

            // Act
            var gate = properties.Match(chainName);

            // Assert
            gate.ToString().Should().Be(expected);
        }

        [Fact]
        public void Match_ForAChainOnTheElementItself_RunsTheComposedPart()
        {
            // Arrange
            var properties = new ValidationProperties("Workshop");

            // Act
            var gate = properties.Match(string.Empty);

            // Assert
            gate.Should().Be(ChainGate.Composed);
        }

        [Fact]
        public void Below_ForAChainOnTheWay_IsWhatLiesBelowIt()
        {
            // Arrange
            var properties = new ValidationProperties("Model.Name", "Model.Manufacturer.CountryCode", "Vin");

            // Act
            var below = properties.Below("Model");

            // Assert
            below!.PropertyNames.Should().Equal("Name", "Manufacturer.CountryCode");
        }

        [Fact]
        public void Below_ForAChainAtASelectedName_IsEverything()
        {
            // Arrange
            var properties = new ValidationProperties("Model");

            // Act
            var below = properties.Below("Model");

            // Assert
            below.Should().BeNull();
        }
    }
}
