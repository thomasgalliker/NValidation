namespace NValidation.Tests
{
    /// <summary>
    /// What the data a caller hands over accepts and how a rule finds a value in it. A value is asked for
    /// by its type, so a type that would answer twice is refused rather than answered by whichever came
    /// first.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class ValidationDataTests
    {
        [Fact]
        public void Constructor_WithValues_KeepsThemInOrder()
        {
            // Arrange
            var policy = new ListingPolicy(200_000, false);

            // Act
            var data = new ValidationData(policy, 42);

            // Assert
            data.Should().Equal(policy, 42);
            data.Count.Should().Be(2);
        }

        [Fact]
        public void Constructor_WithANullValue_Throws()
        {
            // Act
            var act = () => new ValidationData(new ListingPolicy(200_000, false), null!);

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("values");
        }

        [Fact]
        public void Constructor_WithTwoValuesOfTheSameType_Throws()
        {
            // Act
            var act = () => new ValidationData(new ListingPolicy(200_000, false), new ListingPolicy(100_000, true));

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("values").WithMessage("*ListingPolicy*");
        }

        [Fact]
        public void CollectionExpression_WithValues_HandsThemOver()
        {
            // Act
            ValidationData data = [new ListingPolicy(200_000, false)];

            // Assert
            data.TryGet<ListingPolicy>(out var policy).Should().BeTrue();
            policy.Should().Be(new ListingPolicy(200_000, false));
        }

        [Fact]
        public void CollectionExpression_WithoutAValue_IsEmpty()
        {
            // Act
            ValidationData data = [];

            // Assert
            data.Should().BeSameAs(ValidationData.Empty);
        }

        [Fact]
        public void TryGet_ForATypeNoValueIs_ReturnsFalse()
        {
            // Arrange
            var data = new ValidationData(42);

            // Act
            var found = data.TryGet<ListingPolicy>(out var policy);

            // Assert
            found.Should().BeFalse();
            policy.Should().BeNull();
        }

        [Fact]
        public void TryGet_ForAValueType_FindsIt()
        {
            // Arrange
            var data = new ValidationData(new ListingPolicy(200_000, false), 42);

            // Act
            var found = data.TryGet<int>(out var value);

            // Assert
            found.Should().BeTrue();
            value.Should().Be(42);
        }

        [Fact]
        public void TryGet_ForAnInterfaceOneValueImplements_FindsIt()
        {
            // Arrange
            var market = new Market("CH");
            var data = new ValidationData(new ListingPolicy(200_000, false), market);

            // Act
            var found = data.TryGet<IMarket>(out var value);

            // Assert
            found.Should().BeTrue();
            value.Should().BeSameAs(market);
        }

        /// <summary>
        /// Two values answering the same question would make the answer depend on the order the caller
        /// happened to write them in, so the rule asking is told instead.
        /// </summary>
        [Fact]
        public void TryGet_ForATypeTwoValuesAre_Throws()
        {
            // Arrange
            var data = new ValidationData(new Market("CH"), new OtherMarket("DE"));

            // Act
            var act = () => data.TryGet<IMarket>(out _);

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("*Market*OtherMarket*IMarket*");
        }

        [Fact]
        public void ToString_NamesTheTypesOfTheValues()
        {
            // Act
            var text = new ValidationData(new ListingPolicy(200_000, false), 42).ToString();

            // Assert
            text.Should().Be("ListingPolicy, Int32");
        }

        [Fact]
        public void ToString_ForEmpty_SaysNone()
        {
            // Assert
            ValidationData.Empty.ToString().Should().Be("None");
        }

        private interface IMarket
        {
            string Code { get; }
        }

        private sealed record Market(string Code) : IMarket;

        private sealed record OtherMarket(string Code) : IMarket;
    }
}
