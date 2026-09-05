namespace NValidation.Tests
{
    /// <summary>
    /// The chainable builder a rule is appended to.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public partial class PropertyRuleBuilderTests
    {
        /// <summary>
        /// The builder is a struct, so a caller can write <c>default</c> — which carries no rule to
        /// append to. Saying so beats a null reference from somewhere inside the library.
        /// </summary>
        [Fact]
        public void Add_WhenTheBuilderIsDefault_SaysWhereOneComesFrom()
        {
            // Arrange
            var builder = default(PropertyRuleBuilder<Car, string?>);

            // Act
            var act = () => builder.NotEmpty();

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("*Property*");
        }
    }
}
