namespace NValidation.Tests
{
    public partial class PropertyRuleBuilderTests
    {
        /// <summary>
        /// A chain is frozen with the validator it belongs to: a rule appended through a builder kept
        /// from before the first validation is refused rather than silently ignored.
        /// </summary>
        [Fact]
        public async Task Add_AfterTheFirstValidation_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            var chain = validator.Property(c => c.Vin).NotEmpty();

            await validator.ValidateAsync(Cars.Car());

            // Act
            var act = () => chain.MaximumLength(3);

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("*'Vin'*already been used*");
        }

        [Fact]
        public async Task WithMessage_AfterTheFirstValidation_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            var chain = validator.Property(c => c.Vin).NotEmpty();

            await validator.ValidateAsync(Cars.Car());

            // Act
            var act = () => chain.WithMessage("too late");

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("*'Vin'*already been used*");
        }
    }
}
