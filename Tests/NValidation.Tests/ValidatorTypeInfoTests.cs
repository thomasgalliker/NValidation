using System.Reflection;
using NValidation.Internals;

namespace NValidation.Tests
{
    /// <summary>
    /// Works out what a validator validates, so a scan can register it under the right service type.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class ValidatorTypeInfoTests
    {
        /// <summary>
        /// An assembly which references something that was not deployed still loads most of its types.
        /// Scanning takes what loaded rather than failing the application's startup.
        /// </summary>
        [Fact]
        public void GetLoadableTypes_WhenSomeTypesFailToLoad_ReturnsTheOnesThatLoaded()
        {
            // Arrange
            var loaded = typeof(CarValidator);
            var exception = new ReflectionTypeLoadException([loaded, null], [null!, new TypeLoadException()]);

            // Act
            var types = ValidatorTypeInfo.GetLoadableTypes(() => throw exception);

            // Assert
            types.Should().ContainSingle().Which.Should().Be(loaded);
        }

        [Fact]
        public void GetValidatedTypes_ForAValidator_ReportsWhatItValidates()
        {
            // Act
            var validatedTypes = ValidatorTypeInfo.GetValidatedTypes(typeof(CarValidator));

            // Assert
            validatedTypes.Should().ContainSingle().Which.Should().Be(typeof(IValidator<Car>));
        }

        [Fact]
        public void GetValidatedTypes_ForATypeThatIsNotAValidator_ReportsNothing()
        {
            // Act
            var validatedTypes = ValidatorTypeInfo.GetValidatedTypes(typeof(Car));

            // Assert
            validatedTypes.Should().BeEmpty();
        }
    }
}
