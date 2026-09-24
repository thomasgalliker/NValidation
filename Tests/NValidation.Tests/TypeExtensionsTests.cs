namespace NValidation.Tests
{
    /// <summary>
    /// How a type is named in a message a developer reads. The reflection name carries an arity suffix
    /// and no type arguments, which is exactly what a message about a generic type has to report.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class TypeExtensionsTests
    {
        [Fact]
        public void GetFormattedName_WithANonGenericType_ReturnsTheName()
        {
            // Act
            var name = typeof(Car).GetFormattedName();

            // Assert
            name.Should().Be("Car");
        }

        [Fact]
        public void GetFormattedName_WithAClosedGenericType_NamesTheTypeArguments()
        {
            // Act
            var name = typeof(PropertyRuleBuilder<Car, string?>).GetFormattedName();

            // Assert
            name.Should().Be("PropertyRuleBuilder<Car, String>");
        }

        [Fact]
        public void GetFormattedName_WithNestedTypeArguments_FormatsThemTheSameWay()
        {
            // Act
            var name = typeof(Dictionary<string, List<Car>>).GetFormattedName();

            // Assert
            name.Should().Be("Dictionary<String, List<Car>>");
        }

        /// <summary>
        /// An open definition keeps its type parameters where a constructed type keeps its arguments, so
        /// a message about <c>IValidator&lt;&gt;</c> can name the parameter it is missing an argument for.
        /// </summary>
        [Fact]
        public void GetFormattedName_WithAnOpenGenericType_NamesTheTypeParameters()
        {
            // Act
            var name = typeof(IValidator<>).GetFormattedName();

            // Assert
            name.Should().Be("IValidator<T>");
        }

        /// <summary>
        /// A type nested in a generic type reports the declaring type's arguments as its own. They are
        /// not its to spell out, and slicing them off the front is what keeps the name honest.
        /// </summary>
        [Fact]
        public void GetFormattedName_WithATypeNestedInAGenericType_NamesOnlyItsOwnTypeArguments()
        {
            // Act
            var nonGeneric = typeof(Box<int>.Lid).GetFormattedName();
            var generic = typeof(Box<int>.Tag<string>).GetFormattedName();

            // Assert
            nonGeneric.Should().Be("Lid");
            generic.Should().Be("Tag<String>");
        }

        [Fact]
        public void GetFormattedName_WithAnArrayType_KeepsTheBrackets()
        {
            // Act
            var name = typeof(List<Car>[]).GetFormattedName();

            // Assert
            name.Should().Be("List<Car>[]");
        }

        /// <summary>
        /// Where a message names a type the reader has to go and find, the namespace is the part which
        /// tells two same-named validators apart.
        /// </summary>
        [Fact]
        public void GetFormattedFullName_QualifiesTheTypeAndItsArguments()
        {
            // Act
            var name = typeof(Validator<Car>).GetFormattedFullName();

            // Assert
            name.Should().Be("NValidation.Validator<NValidation.TestData.Car>");
        }

        [Fact]
        public void GetFormattedFullName_WithATypeParameter_LeavesItUnqualified()
        {
            // Act
            var name = typeof(IValidator<>).GetFormattedFullName();

            // Assert
            name.Should().Be("NValidation.IValidator<T>");
        }

        [Fact]
        public void GetFormattedFullName_WithANestedType_NamesTheDeclaringType()
        {
            // Act
            var name = typeof(Box<int>.Lid).GetFormattedFullName();

            // Assert
            name.Should().Be("NValidation.Tests.TypeExtensionsTests.Box<TItem>.Lid");
        }

        private sealed class Box<TItem>
        {
            internal sealed class Lid;

            internal sealed class Tag<TLabel>;
        }
    }
}
