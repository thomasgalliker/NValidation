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

            foreach (var messageKey in ValidationMessageProviderAssertions.CoreMessageKeys())
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

        /// <summary>
        /// Has a message at all, and names no placeholder a rule does not supply. Both halves are what
        /// <see cref="ValidationMessageProviderAssertions.ShouldResolveMessageKey"/> promises any provider,
        /// so the built-in one is held to the same bar an application's own is.
        /// </summary>
        [Theory]
        [MemberData(nameof(MessageKeys))]
        public void GetMessage_ResolvesEveryKeyOfTheCore(string messageKey)
        {
            // Act
            var act = () => DefaultValidationMessageProvider.Instance.ShouldResolveMessageKey(messageKey);

            // Assert
            act.Should().NotThrow();
        }

        /// <summary>
        /// A house rule of the built-in English rather than a requirement of the seam: a message read on
        /// its own has no labelled input next to it, while a translation shown underneath one is free to
        /// leave the property out.
        /// </summary>
        [Theory]
        [MemberData(nameof(MessageKeys))]
        public void GetMessage_NamesTheFailingProperty_ForEveryKeyOfTheCore(string messageKey)
        {
            // Arrange
            const string propertyName = "TheFailingProperty";

            // Act
            var message = DefaultValidationMessageProvider.Instance.GetMessage(
                messageKey,
                ValidationMessageProviderAssertions.CorePlaceholderArguments(propertyName));

            // Assert
            message.Should().Contain(propertyName, $"{messageKey} must name the failing property");
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
    }
}
