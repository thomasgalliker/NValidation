namespace NValidation.Tests
{
    /// <summary>
    /// The English fallback which makes the validation core usable without a host supplying messages.
    /// These texts name the failing property, since a message read on its own has no labelled input
    /// next to it.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class DefaultValidationMessageProviderTests
    {
        /// <summary>
        /// Every key the core can report, so a rule added without its message is caught here rather than
        /// by a caller reading the raw key in a response.
        /// </summary>
        public static TheoryData<string> MessageKeys()
        {
            var messageKeys = new TheoryData<string>();

            foreach (var messageKey in DeclaredConstants(typeof(ValidationMessageKeys)))
            {
                messageKeys.Add(messageKey);
            }

            return messageKeys;
        }

        [Fact]
        public void GetMessage_FormatsThePropertyNameAndTheRuleArguments()
        {
            // Act
            var message = DefaultValidationMessageProvider.Instance.GetMessage(
                ValidationMessageKeys.MaximumLength,
                "Name",
                (ValidationMessagePlaceholders.MaxLength, 200));

            // Assert
            message.Should().Be("Name must not exceed 200 characters.");
        }

        [Theory]
        [MemberData(nameof(MessageKeys))]
        public void GetMessage_HasABuiltInMessage_ForEveryKeyOfTheCore(string messageKey)
        {
            // Act
            var message = DefaultValidationMessageProvider.Instance.GetMessage(messageKey, PlaceholderArguments.All());

            // Assert
            message.Should().NotBe(messageKey, $"{messageKey} must have a built-in English message");
        }

        [Theory]
        [MemberData(nameof(MessageKeys))]
        public void GetMessage_NamesTheFailingProperty_ForEveryKeyOfTheCore(string messageKey)
        {
            // Act
            var message = DefaultValidationMessageProvider.Instance.GetMessage(messageKey, PlaceholderArguments.All());

            // Assert
            message.Should().Contain(PlaceholderArguments.PropertyName, $"{messageKey} must name the failing property");
        }

        [Theory]
        [MemberData(nameof(MessageKeys))]
        public void GetMessage_NamesOnlyDeclaredPlaceholders_ForEveryKeyOfTheCore(string messageKey)
        {
            // Act
            var message = DefaultValidationMessageProvider.Instance.GetMessage(messageKey, PlaceholderArguments.All());

            // Assert
            message.Should().NotMatchRegex(
                PlaceholderArguments.UnresolvedPlaceholderPattern,
                $"{messageKey} must only name placeholders declared in {nameof(ValidationMessagePlaceholders)}");
        }

        /// <summary>
        /// An unmapped key still produces something readable rather than failing the request.
        /// </summary>
        [Fact]
        public void GetMessage_FallsBackToTheKey_WhenItIsUnknown()
        {
            // Act
            var message = DefaultValidationMessageProvider.Instance.GetMessage("SomeRuleWithoutAMessage", "Name");

            // Assert
            message.Should().Be("SomeRuleWithoutAMessage");
        }

        private static IEnumerable<string> DeclaredConstants(Type type)
        {
            return type
                .GetFields()
                .Where(field => field.IsLiteral && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue()!);
        }
    }
}
