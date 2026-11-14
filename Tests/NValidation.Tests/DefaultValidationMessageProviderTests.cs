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
        public static TheoryData<string> ErrorCodes()
        {
            var errorCodes = new TheoryData<string>();

            foreach (var errorCode in ValidationMessageProviderAssertions.CoreErrorCodes())
            {
                errorCodes.Add(errorCode);
            }

            return errorCodes;
        }

        [Fact]
        public void GetMessage_FormatsThePropertyNameAndTheRuleArguments()
        {
            // Act
            var message = DefaultValidationMessageProvider.Instance.GetMessage(
                ValidationErrorCodes.MaximumLength,
                "Name",
                (ValidationMessagePlaceholders.MaxLength, 200));

            // Assert
            message.Should().Be("Name must not exceed 200 characters.");
        }

        /// <summary>
        /// Has a message at all, and names no placeholder a rule does not supply. Both halves are what
        /// <see cref="ValidationMessageProviderAssertions.ShouldResolveErrorCode"/> promises any provider,
        /// so the built-in one is held to the same bar an application's own is.
        /// </summary>
        [Theory]
        [MemberData(nameof(ErrorCodes))]
        public void GetMessage_ResolvesEveryKeyOfTheCore(string errorCode)
        {
            // Act
            var act = () => DefaultValidationMessageProvider.Instance.ShouldResolveErrorCode(errorCode);

            // Assert
            act.Should().NotThrow();
        }

        /// <summary>
        /// A house rule of the built-in English rather than a requirement of the seam: a message read on
        /// its own has no labelled input next to it, while a translation shown underneath one is free to
        /// leave the property out.
        /// </summary>
        [Theory]
        [MemberData(nameof(ErrorCodes))]
        public void GetMessage_NamesTheFailingProperty_ForEveryKeyOfTheCore(string errorCode)
        {
            // Arrange
            const string propertyName = "TheFailingProperty";

            // Act
            var message = DefaultValidationMessageProvider.Instance.GetMessage(
                errorCode,
                ValidationMessageProviderAssertions.CorePlaceholderArguments(propertyName));

            // Assert
            message.Should().Contain(propertyName, $"{errorCode} must name the failing property");
        }

        /// <summary>
        /// The built-in English is correct at its boundary values, not only at plural ones.
        /// </summary>
        [Theory]
        [InlineData(1, "Name must be at least 1 character long.")]
        [InlineData(2, "Name must be at least 2 characters long.")]
        public void GetMessage_UsesTheSingularForACountOfOne(int minimumLength, string expected)
        {
            // Act
            var message = DefaultValidationMessageProvider.Instance.GetMessage(
                ValidationErrorCodes.MinimumLength,
                "Name",
                (ValidationMessagePlaceholders.MinLength, minimumLength));

            // Assert
            message.Should().Be(expected);
        }

        [Theory]
        [InlineData(1, "Features must contain at least 1 entry.")]
        [InlineData(3, "Features must contain at least 3 entries.")]
        public void GetMessage_UsesTheSingularForACollectionOfOne(int minimumCount, string expected)
        {
            // Act
            var message = DefaultValidationMessageProvider.Instance.GetMessage(
                ValidationErrorCodes.MinimumCount,
                "Features",
                (ValidationMessagePlaceholders.MinCount, minimumCount));

            // Assert
            message.Should().Be(expected);
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
