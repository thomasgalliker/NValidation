namespace NValidation.Tests
{
    /// <summary>
    /// The promise that a chain is always built for the nullable form of the property's type. Half of it is
    /// asserted by the compiler — every chain here is declared on a non-nullable property, so a
    /// <c>Property</c> that stopped widening would not build — and the rest is what the rules then do with
    /// the null that such a property can still carry off the wire.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class NullableAnnotationsTests
    {
        [Fact]
        public async Task Property_WithNonNullableReferenceProperties_JudgesTheValueThatArrived()
        {
            // Arrange
            var validator = new TestValidator<NonNullablePayload>();
            validator.Property(p => p.Reference).NotEmpty().MinimumLength(3).Matches("^[A-Z]+$");
            validator.Property(p => p.Manufacturer).NotNull().SetValidator(new ManufacturerValidator());
            validator.Property(p => p.Tags).NotEmpty();

            var payload = new NonNullablePayload { Reference = null!, Manufacturer = null!, Tags = null! };

            // Act
            var result = await validator.ValidateAsync(payload);

            // Assert
            result.ShouldReport([
                new("Reference", "Reference is required."),
                new("Manufacturer", "Manufacturer is required."),
                new("Tags", "Tags is required.")]);
        }

        [Fact]
        public async Task Property_WithANonNullableReferenceProperty_HandsTheRuleTheValueAsItIs()
        {
            // Arrange
            var seen = new List<string?>();

            var validator = new TestValidator<NonNullablePayload>();
            validator.Property(p => p.Reference).Must(value =>
            {
                seen.Add(value);

                return true;
            });

            var payload = new NonNullablePayload { Reference = null! };

            // Act
            var result = await validator.ValidateAsync(payload);

            // Assert
            result.Errors.Should().BeEmpty();
            seen.Should().ContainSingle().Which.Should().BeNull();
        }
    }
}
