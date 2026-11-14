namespace NValidation.Tests
{
    /// <summary>
    /// The chainable builder a rule is appended to.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public partial class PropertyRuleBuilderTests
    {
        /// <summary>
        /// A rule may return a builder derived from the plain one, carrying refinements of its own. The
        /// rules written after it still chain — they are extension methods on the base — and judge the
        /// same property: the refinement admits the value, and only the rule after it objects.
        /// </summary>
        [Fact]
        public async Task ADerivedBuilder_ContinuesTheChainOfTheSameProperty()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.ContactEmail)
                .EmailAddress()
                .AllowQuotedLocalPart()
                .MaximumLength(24);

            var manufacturer = Cars.Manufacturer();
            manufacturer.ContactEmail = "\"john and jane doe\"@example.com";

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.ShouldReportErrorCode("ContactEmail", "MaximumLength");
        }
    }
}
