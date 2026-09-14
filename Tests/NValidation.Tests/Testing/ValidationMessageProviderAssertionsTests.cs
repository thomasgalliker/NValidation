namespace NValidation.Tests.Testing
{
    /// <summary>
    /// The conformance assertion an application writes once for its own message provider.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class ValidationMessageProviderAssertionsTests
    {
        /// <summary>
        /// The shipped list is written out rather than reflected, because a trimmer may drop the metadata
        /// of constants that were inlined. This is what keeps it honest: the test assembly is never
        /// trimmed, so a key added without being listed turns the suite red here.
        /// </summary>
        [Fact]
        public void CoreMessageKeys_ListsEveryKeyTheCoreDeclares()
        {
            // Act
            var listed = ValidationMessageProviderAssertions.CoreMessageKeys();

            // Assert
            listed.Should().BeEquivalentTo(DeclaredConstants(typeof(ValidationMessageKeys)));
        }

        /// <inheritdoc cref="CoreMessageKeys_ListsEveryKeyTheCoreDeclares" path="/summary"/>
        [Fact]
        public void CoreMessagePlaceholders_ListsEveryPlaceholderTheCoreDeclares()
        {
            // Act
            var listed = ValidationMessageProviderAssertions.CoreMessagePlaceholders();

            // Assert
            listed.Should().BeEquivalentTo(DeclaredConstants(typeof(ValidationMessagePlaceholders)));
        }

        [Fact]
        public void CorePlaceholderArguments_NamesTheFailingProperty()
        {
            // Act
            var arguments = ValidationMessageProviderAssertions.CorePlaceholderArguments("Vin");

            // Assert
            arguments[ValidationMessagePlaceholders.PropertyName].Should().Be("Vin");
            arguments.Should().HaveSameCount(ValidationMessageProviderAssertions.CoreMessagePlaceholders());
        }

        [Fact]
        public void ShouldResolveEveryCoreMessageKey_ForTheBuiltInProvider_Passes()
        {
            // Act
            var act = () => DefaultValidationMessageProvider.Instance.ShouldResolveEveryCoreMessageKey();

            // Assert
            act.Should().NotThrow();
        }

        /// <summary>
        /// A provider which answers with the key has no message, which is the failure that would otherwise
        /// reach a caller as a raw key in a response.
        /// </summary>
        [Fact]
        public void ShouldResolveMessageKey_WhenTheProviderHasNoMessage_Fails()
        {
            // Act
            var act = () => MessageKeyProvider.Instance.ShouldResolveMessageKey(ValidationMessageKeys.NotEmpty);

            // Assert
            act.Should().Throw<ValidationAssertionException>()
                .Which.Message.Should().Contain("NotEmpty").And.Contain("answered with the key itself");
        }

        [Fact]
        public void ShouldResolveEveryCoreMessageKey_ReportsEveryKeyThatFails_NotOnlyTheFirst()
        {
            // Act
            var act = () => MessageKeyProvider.Instance.ShouldResolveEveryCoreMessageKey();

            // Assert
            act.Should().Throw<ValidationAssertionException>()
                .Which.Message.Should().Contain(
                    $"{ValidationMessageProviderAssertions.CoreMessageKeys().Count} failed");
        }

        [Theory]
        [InlineData("{MaxLength} is a placeholder nothing supplied")]
        [InlineData("with a format: {Step:0.00}")]
        public void ShouldResolveMessageKey_WhenAPlaceholderSurvives_Fails(string message)
        {
            // Arrange
            var provider = new FixedMessageProvider(message);

            // Act
            var act = () => provider.ShouldResolveMessageKey("SomeKey");

            // Assert
            act.Should().Throw<ValidationAssertionException>()
                .Which.Message.Should().Contain("SomeKey");
        }

        /// <summary>
        /// Braces are not always a placeholder. A message is free to carry text in them, and a provider
        /// which does must not be called broken for it.
        /// </summary>
        [Theory]
        [InlineData("nothing in braces at all")]
        [InlineData("a brace with {two words} inside")]
        [InlineData("an unclosed { brace")]
        [InlineData("an empty {} pair")]
        public void ShouldResolveMessageKey_WithBracesThatAreNotPlaceholders_Passes(string message)
        {
            // Arrange
            var provider = new FixedMessageProvider(message);

            // Act
            var act = () => provider.ShouldResolveMessageKey("SomeKey");

            // Assert
            act.Should().NotThrow();
        }

        private static IEnumerable<string> DeclaredConstants(Type type)
        {
            return type
                .GetFields()
                .Where(field => field.IsLiteral && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue()!);
        }

        private sealed class FixedMessageProvider : IValidationMessageProvider
        {
            private readonly string message;

            public FixedMessageProvider(string message)
            {
                this.message = message;
            }

            public string GetMessage(string messageKey, IReadOnlyDictionary<string, object?> arguments)
            {
                return this.message;
            }
        }
    }
}
