namespace NValidation.Tests
{
    /// <summary>
    /// The named-placeholder substitution which decouples a rule's arguments from the wording of its
    /// message: the rule always supplies everything it has, and the message — a translation, typically —
    /// decides what to mention.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class ValidationMessageFormatterTests
    {
        [Fact]
        public void Format_SubstitutesTheNamedPlaceholders()
        {
            // Arrange
            var arguments = Arguments((ValidationMessagePlaceholders.PropertyName, "Name"), (ValidationMessagePlaceholders.MaxLength, 200));

            // Act
            var message = ValidationMessageFormatter.Format("{PropertyName} must not exceed {MaxLength} characters.", arguments);

            // Assert
            message.Should().Be("Name must not exceed 200 characters.");
        }

        /// <summary>
        /// The reason a message may leave the property out: an argument nobody references costs nothing.
        /// </summary>
        [Fact]
        public void Format_IgnoresArgumentsTheTemplateDoesNotName()
        {
            // Arrange
            var arguments = Arguments((ValidationMessagePlaceholders.PropertyName, "Name"), (ValidationMessagePlaceholders.MaxLength, 200));

            // Act
            var message = ValidationMessageFormatter.Format("Must not exceed {MaxLength} characters.", arguments);

            // Assert
            message.Should().Be("Must not exceed 200 characters.");
        }

        /// <summary>
        /// A typo in a translation must not throw on a request — unlike positional formatting, where an
        /// index the arguments do not cover is a <see cref="FormatException"/>.
        /// </summary>
        [Fact]
        public void Format_KeepsAPlaceholderTheArgumentsDoNotCover()
        {
            // Arrange
            var arguments = Arguments((ValidationMessagePlaceholders.PropertyName, "Name"));

            // Act
            var message = ValidationMessageFormatter.Format("{PropertyName} must not exceed {MaxLenght} characters.", arguments);

            // Assert
            message.Should().Be("Name must not exceed {MaxLenght} characters.");
        }

        [Fact]
        public void Format_AppliesTheFormatStringOfAPlaceholder()
        {
            // Arrange
            var arguments = Arguments((ValidationMessagePlaceholders.Step, 0.5m));

            // Act
            var message = ValidationMessageFormatter.Format("Must be a multiple of {Step:0.00}.", arguments);

            // Assert
            message.Should().Be("Must be a multiple of 0.50.");
        }

        [Fact]
        public void Format_RendersANullArgumentAsAnEmptyString()
        {
            // Arrange
            var arguments = Arguments((ValidationMessagePlaceholders.OtherPropertyName, null));

            // Act
            var message = ValidationMessageFormatter.Format("Must not be earlier than {OtherPropertyName}.", arguments);

            // Assert
            message.Should().Be("Must not be earlier than .");
        }

        /// <summary>
        /// Braces carry no special meaning beyond a placeholder, so a message does not have to escape
        /// them the way a positional format string does.
        /// </summary>
        [Fact]
        public void Format_LeavesTextWithoutPlaceholdersUnchanged()
        {
            // Act
            var message = ValidationMessageFormatter.Format("This field is required.", Arguments((ValidationMessagePlaceholders.PropertyName, "Name")));

            // Assert
            message.Should().Be("This field is required.");
        }

        private static IReadOnlyDictionary<string, object?> Arguments(params (string Name, object? Value)[] arguments)
        {
            return arguments.ToDictionary(argument => argument.Name, argument => argument.Value, StringComparer.Ordinal);
        }
    }
}
