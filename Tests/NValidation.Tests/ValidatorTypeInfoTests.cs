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

        /// <summary>
        /// An open generic validator implements <see cref="IValidator{T}"/> for a type parameter rather
        /// than for a payload, so there is no service type to register it under until it is closed. This
        /// assembly contains one — <see cref="TestData.TestValidator{T}"/>, which every test declares its
        /// rules on — so a scan that stopped skipping them would try to register it and take the host's
        /// startup with it.
        /// </summary>
        [Fact]
        public void GetValidatedTypes_ForAnOpenGenericValidator_ReportsNothing()
        {
            // Act
            var validatedTypes = ValidatorTypeInfo.GetValidatedTypes(typeof(TestData.TestValidator<>));

            // Assert
            validatedTypes.Should().BeEmpty();
        }

        /// <summary>
        /// A closed generic validator is a validator like any other: nothing about having been generic
        /// disqualifies it once the payload is known.
        /// </summary>
        [Fact]
        public void GetValidatedTypes_ForAClosedGenericValidator_ReportsWhatItValidates()
        {
            // Act
            var validatedTypes = ValidatorTypeInfo.GetValidatedTypes(typeof(TestData.TestValidator<Car>));

            // Assert
            validatedTypes.Should().ContainSingle().Which.Should().Be(typeof(IValidator<Car>));
        }

        /// <summary>
        /// The base class itself is abstract, so it declares no rules and there is nothing to validate
        /// with. A scan which registered it would hand callers a validator that passes everything.
        /// </summary>
        [Fact]
        public void GetValidatedTypes_ForAnAbstractValidator_ReportsNothing()
        {
            // Act
            var validatedTypes = ValidatorTypeInfo.GetValidatedTypes(typeof(Validator<Car>));

            // Assert
            validatedTypes.Should().BeEmpty();
        }

        /// <summary>
        /// And the interface a scan looks for is not itself something to register. What excludes it is the
        /// same abstract check that excludes <see cref="Validator{T}"/> — the CLI requires an interface
        /// definition to carry the abstract flag — so this needs no test of its own in the guard, and a
        /// separate <c>IsInterface</c> clause there could never be the deciding one.
        /// </summary>
        [Fact]
        public void GetValidatedTypes_ForTheValidatorInterface_ReportsNothing()
        {
            // Act
            var validatedTypes = ValidatorTypeInfo.GetValidatedTypes(typeof(IValidator<Car>));

            // Assert
            validatedTypes.Should().BeEmpty();
        }
    }
}
