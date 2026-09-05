namespace NValidation.Tests
{
    /// <summary>
    /// The cache behind <c>Validator&lt;T&gt;.Property</c>, which compiles a property expression once and
    /// reuses the delegate.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class PropertyAccessorTests
    {
        /// <summary>
        /// The property path is the same for <c>x =&gt; x.Mileage</c> and <c>x =&gt; (object)x.Mileage</c>,
        /// because a conversion is stripped when the code is derived. The compiled accessors are not
        /// interchangeable, so the property type has to be part of what the cache is keyed on.
        /// </summary>
        [Fact]
        public async Task For_DistinguishesTheSamePath_ReachedWithADifferentPropertyType()
        {
            // Arrange
            var typed = new MileageGreaterThanValidator(0);
            var boxed = new MileageAsObjectValidator();
            await typed.ValidateAsync(Cars.Car());

            // Act
            var act = () => boxed.ValidateAsync(Cars.Car()).AsTask();

            // Assert
            await act.Should().NotThrowAsync();
        }
    }
}
