namespace NValidation.Tests
{
    /// <summary>
    /// What a rule may be declared for. The path an expression produces is the error code, and it is
    /// also the key the compiled accessor and the reachability guard are cached under — so an expression
    /// whose path does not identify it has to be refused where it is written, not silently share another
    /// rule's delegate at run time.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class PropertyPathTests
    {
        /// <summary>
        /// <c>c => c.ServiceInvoiceNumbers[0].Length</c> and <c>c => c.ServiceInvoiceNumbers[1].Length</c>
        /// both reduce to "Length": the indexer is not a member, so the walk stops there. Sharing that
        /// path made the second rule read the first rule's element and report nothing.
        /// </summary>
        [Fact]
        public void Property_WithAPathThroughAnIndexer_Throws()
        {
            // Act
            var act = () => new IndexedPathValidator();

            // Assert
            act.Should().Throw<ArgumentException>().WithMessage("*must select a property*");
        }

        [Fact]
        public void Property_WithAPathThroughAMethodCall_Throws()
        {
            // Act
            var act = () => new MethodCallPathValidator();

            // Assert
            act.Should().Throw<ArgumentException>().WithMessage("*must select a property*");
        }

        /// <summary>
        /// A rule declared for something the validated object does not own is not a rule about the
        /// payload, however plausible the code it would report under looks.
        /// </summary>
        [Fact]
        public void Property_WithAPathRootedInACapturedObject_Throws()
        {
            // Act
            var act = () => new CapturedPathValidator(Cars.Manufacturer());

            // Assert
            act.Should().Throw<ArgumentException>().WithMessage("*through its own parameter*");
        }

        [Fact]
        public void Property_WithAPathRootedInAStaticMember_Throws()
        {
            // Act
            var act = () => new StaticPathValidator();

            // Assert
            act.Should().Throw<ArgumentException>().WithMessage("*through its own parameter*");
        }

        /// <summary>
        /// The paths that are refused above are the only ones: a nested path through the parameter is
        /// what the library is for.
        /// </summary>
        [Fact]
        public async Task Property_WithANestedPathThroughTheParameter_IsAccepted()
        {
            // Arrange
            var validator = new ManufacturerNameNotEmptyValidator();
            var car = Cars.Car();
            car.Model!.Manufacturer!.Name = null;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Errors.Should().ContainSingle().Which.Code.Should().Be("Model.Manufacturer.Name");
        }
    }
}
