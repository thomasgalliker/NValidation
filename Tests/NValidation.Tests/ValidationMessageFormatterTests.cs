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
        /// A format specifier is written by whoever writes the message, while the value it lands on comes
        /// from whichever rule reported the key — and one key serves several CLR types, so a specifier
        /// that is legal for a date is not for a decimal. Rendering the value plainly is the answer,
        /// because throwing here would turn a bad request into a server error.
        /// </summary>
        [Fact]
        public void Format_FallsBackToThePlainRendering_WhenTheValueCannotHonourTheFormat()
        {
            // Arrange
            var arguments = Arguments((ValidationMessagePlaceholders.OtherValue, 12.5m));

            // Act
            var message = ValidationMessageFormatter.Format("Must be before {OtherValue:d}.", arguments);

            // Assert
            message.Should().Be("Must be before 12.5.");
        }

        /// <summary>
        /// The same for a specifier a type rejects with an <see cref="ArgumentException"/> rather than a
        /// <see cref="FormatException"/>.
        /// </summary>
        [Fact]
        public void Format_FallsBackToThePlainRendering_WhenTheFormatIsNotAKnownOne()
        {
            // Arrange
            var arguments = Arguments((ValidationMessagePlaceholders.OtherValue, new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc)));

            // Act
            var message = ValidationMessageFormatter.Format("Must be before {OtherValue:Q}.", arguments);

            // Assert
            message.Should().NotBeEmpty();
            message.Should().StartWith("Must be before ");
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

        /// <summary>
        /// A count of one takes the first form. Without this the most common collection rule reads
        /// "must contain at least 1 entries", which looks like a bug in the library.
        /// </summary>
        [Theory]
        [InlineData(0, "0 entries")]
        [InlineData(1, "1 entry")]
        [InlineData(2, "2 entries")]
        [InlineData(17, "17 entries")]
        public void Format_SelectsTheFormMatchingTheCount(int count, string expected)
        {
            // Arrange
            var arguments = Arguments((ValidationMessagePlaceholders.MinCount, count));

            // Act
            var message = ValidationMessageFormatter.Format("{MinCount} {MinCount:entry|entries}", arguments);

            // Assert
            message.Should().Be(expected);
        }

        /// <summary>
        /// A bar is a legal literal in a custom numeric format string, so a format is only read as a
        /// selector when the argument is a whole number.
        /// </summary>
        [Fact]
        public void Format_LeavesANonNumericArgumentToItsFormat()
        {
            // Arrange
            var arguments = Arguments((ValidationMessagePlaceholders.PropertyName, "Name"));

            // Act
            var message = ValidationMessageFormatter.Format("{PropertyName:one|many}", arguments);

            // Assert
            message.Should().Be("Name");
        }

        [Fact]
        public void Format_StillAppliesAnOrdinaryNumericFormat()
        {
            // Arrange
            var arguments = Arguments((ValidationMessagePlaceholders.MaxCount, 1200));

            // Act
            var message = ValidationMessageFormatter.Format("{MaxCount:#,##0}", arguments);

            // Assert
            message.Should().Be(1200.ToString("#,##0", System.Globalization.CultureInfo.CurrentCulture));
        }

        /// <summary>
        /// More forms than two is malformed. It must not reach the number's own formatting, which
        /// echoes characters it does not recognise and would put the forms verbatim into the message.
        /// A language needing three forms supplies its own provider.
        /// </summary>
        [Fact]
        public void Format_LeavesAFormatWithMoreThanTwoFormsAlone()
        {
            // Arrange
            var arguments = Arguments((ValidationMessagePlaceholders.MinCount, 1));

            // Act
            var message = ValidationMessageFormatter.Format("{MinCount:one|few|many}", arguments);

            // Assert
            message.Should().Be("1");
        }

        private static IReadOnlyDictionary<string, object?> Arguments(params (string Name, object? Value)[] arguments)
        {
            return arguments.ToDictionary(argument => argument.Name, argument => argument.Value, StringComparer.Ordinal);
        }
    }
}
