namespace NValidation.Tests.Testing
{
    /// <summary>
    /// The assertion the whole suite is written on, so its own failures have to be tested: one that
    /// quietly accepts anything would leave every test green while proving nothing.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class ValidationAssertionsTests
    {
        /// <summary>
        /// Every shape a <c>{ propertyName: [messages] }</c> map arrives in has to be assertable. Two
        /// overloads, one on <c>IDictionary</c> and one on <c>IReadOnlyDictionary</c>, would make a
        /// plain <c>Dictionary</c> — which implements both — an ambiguous call that does not compile.
        /// </summary>
        [Fact]
        public void ShouldReport_AcceptsAConcreteDictionary()
        {
            // Arrange
            var errors = new Dictionary<string, string[]> { ["Vin"] = ["Vin is required."] };

            // Act
            var act = () => errors.ShouldReport("Vin", "Vin is required.");

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void ShouldReport_AcceptsAReadOnlyDictionary()
        {
            // Arrange
            IReadOnlyDictionary<string, string[]> errors =
                new Dictionary<string, string[]> { ["Vin"] = ["Vin is required."] };

            // Act
            var act = () => errors.ShouldReport("Vin", "Vin is required.");

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void ShouldReport_AcceptsAMutableDictionaryInterface()
        {
            // Arrange
            IDictionary<string, string[]> errors =
                new Dictionary<string, string[]> { ["Vin"] = ["Vin is required."] };

            // Act
            var act = () => errors.ShouldReport("Vin", "Vin is required.");

            // Assert
            act.Should().NotThrow();
        }

        /// <summary>
        /// The reason an expected message is compared exactly rather than as a pattern: a question mark
        /// is ordinary in user-facing copy, and as a wildcard it would match any character at all — so
        /// this assertion would have passed against a message it never got.
        /// </summary>
        [Fact]
        public void ShouldReport_WithAQuestionMarkInTheExpectedMessage_MatchesItLiterally()
        {
            // Arrange
            var result = Result(("Vin", "Is the VIN correct!"));

            // Act
            var act = () => result.ShouldReport("Vin", "Is the VIN correct?");

            // Assert
            act.Should().Throw<ValidationAssertionException>();
        }

        [Fact]
        public void ShouldReport_WithAQuestionMarkInTheActualMessage_Matches()
        {
            // Arrange
            var result = Result(("Vin", "Is the VIN correct?"));

            // Act
            var act = () => result.ShouldReport("Vin", "Is the VIN correct?");

            // Assert
            act.Should().NotThrow();
        }

        /// <summary>
        /// The same characters still mean what they always did where a pattern is asked for by name.
        /// </summary>
        [Fact]
        public void ShouldReport_Matching_TreatsTheExpectationAsAPattern()
        {
            // Arrange
            var result = Result(("Mileage", "Mileage must be greater than or equal to 0."));

            // Act
            var act = () => result.ShouldReport([ExpectedError.Matching("Mileage", "*greater than*")]);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void ShouldReport_WithAnAsteriskInTheExpectedMessage_MatchesItLiterally()
        {
            // Arrange
            var result = Result(("Vin", "The VIN is required."));

            // Act
            var act = () => result.ShouldReport("Vin", "The VIN *");

            // Assert
            act.Should().Throw<ValidationAssertionException>();
        }

        private static ValidationResult Result(params (string PropertyName, string Message)[] errors)
        {
            return ValidationResult.FromValidationErrors(
                errors.Select(error => new ValidationError(error.PropertyName, error.Message)).ToArray());
        }

        [Fact]
        public void ShouldReport_WithTheExactMessage_Passes()
        {
            // Arrange
            var result = Result(("Vin", "Vin is required."));

            // Act
            var act = () => result.ShouldReport("Vin", "Vin is required.");

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void ShouldReport_WithAFragmentOfTheMessage_Passes()
        {
            // Arrange
            var result = Result(("Mileage", "Mileage must be greater than or equal to 0."));

            // Act
            var act = () => result.ShouldReport([ExpectedError.Matching("Mileage", "*greater than*")]);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void ShouldReport_WithoutAMessage_AcceptsAnyWording()
        {
            // Arrange
            var result = Result(("Vin", "Vin is required."));

            // Act
            var act = () => result.ShouldReport([ExpectedError.Any("Vin")]);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void ShouldReport_IgnoresTheOrderTheErrorsArriveIn()
        {
            // Arrange
            var result = Result(("Vin", "Vin is required."), ("Mileage", "Mileage is required."));

            // Act
            var act = () => result.ShouldReport([
                new("Mileage", "Mileage is required."),
                new("Vin", "Vin is required.")]);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void ShouldReport_WithTheWrongMessage_Fails()
        {
            // Arrange
            var result = Result(("Vin", "Vin is required."));

            // Act
            var act = () => result.ShouldReport("Vin", "Vin is mandatory.");

            // Assert
            act.Should().Throw<ValidationAssertionException>()
                .Which.Message.Should().Contain("Vin is required.").And.Contain("Vin is mandatory.");
        }

        [Fact]
        public void ShouldReport_WithTheWrongCode_Fails()
        {
            // Arrange
            var result = Result(("Vin", "Vin is required."));

            // Act
            var act = () => result.ShouldReport("Vim", "Vin is required.");

            // Assert
            act.Should().Throw<ValidationAssertionException>();
        }

        [Fact]
        public void ShouldReport_WithAnErrorItDoesNotName_Fails()
        {
            // Arrange
            var result = Result(("Vin", "Vin is required."), ("Mileage", "Mileage is required."));

            // Act
            var act = () => result.ShouldReport("Vin", "Vin is required.");

            // Assert
            act.Should().Throw<ValidationAssertionException>()
                .Which.Message.Should().Contain("Not expected:").And.Contain("Mileage");
        }

        [Fact]
        public void ShouldReport_WithAnErrorThatNeverCame_Fails()
        {
            // Arrange
            var result = Result(("Vin", "Vin is required."));

            // Act
            var act = () => result.ShouldReport([
                new("Vin", "Vin is required."),
                new("Mileage", "Mileage is required.")]);

            // Assert
            act.Should().Throw<ValidationAssertionException>()
                .Which.Message.Should().Contain("Not reported:").And.Contain("nothing was reported under \"Mileage\"");
        }

        /// <summary>
        /// The count is part of what is expected, so the same name twice asks for two failures.
        /// </summary>
        [Fact]
        public void ShouldReport_CountsARepeatedCodeTwice()
        {
            // Arrange
            var result = Result(("Vin", "Vin is required."), ("Vin", "Vin must not exceed 3 characters."));

            // Act
            var bothNamed = () => result.ShouldReport([
                new("Vin", "Vin is required."),
                new("Vin", "Vin must not exceed 3 characters.")]);
            var onlyOneNamed = () => result.ShouldReport("Vin", "Vin is required.");

            // Assert
            bothNamed.Should().NotThrow();
            onlyOneNamed.Should().Throw<ValidationAssertionException>();
        }

        [Fact]
        public void ShouldReport_WithARepeatedCode_DoesNotPairOneErrorTwice()
        {
            // Arrange
            var result = Result(("Vin", "Vin is required."), ("Vin", "Vin must not exceed 3 characters."));

            // Act
            var act = () => result.ShouldReport([
                new("Vin", "Vin is required."),
                new("Vin", "Vin is required.")]);

            // Assert
            act.Should().Throw<ValidationAssertionException>();
        }

        /// <summary>
        /// The case a matcher which pairs greedily gets wrong: the bare "Vin" fits either error, so taking
        /// the first one leaves the fragment nothing to pair with even though a complete pairing exists.
        /// </summary>
        [Fact]
        public void ShouldReport_WithOverlappingExpectations_FindsACompletePairing()
        {
            // Arrange
            var result = Result(("Vin", "Vin is required."), ("Vin", "Vin must not exceed 3 characters."));

            // Act
            var act = () => result.ShouldReport([ExpectedError.Any("Vin"), ExpectedError.Matching("Vin", "*not exceed*")]);

            // Assert
            act.Should().NotThrow();
        }

        /// <summary>
        /// Nothing came back, so naming each expectation as missing would only repeat the list.
        /// </summary>
        [Fact]
        public void ShouldReport_WithASuccessfulResult_SaysSoPlainly()
        {
            // Act
            var act = () => ValidationResult.Success.ShouldReport("Vin", "Vin is required.");

            // Assert
            act.Should().Throw<ValidationAssertionException>()
                .Which.Message.Should().Be(
                    """
                    Expected the validation result to report one error:
                      Vin  "Vin is required."
                    but it succeeded.
                    """);
        }

        /// <summary>
        /// The mirror image: nothing was expected, so every error is unexpected and a diff would only
        /// repeat the list.
        /// </summary>
        [Fact]
        public void ShouldReport_WhenNothingWasExpected_ListsWhatCameBack()
        {
            // Arrange
            var result = Result(("Vin", "Vin is required."));

            // Act
            var act = () => result.ShouldReport([]);

            // Assert
            act.Should().Throw<ValidationAssertionException>()
                .Which.Message.Should().Be(
                    """
                    Expected the validation result to succeed, but it reported one error:
                      Vin  "Vin is required."
                    """);
        }

        /// <summary>
        /// The shape a reader sees most often, pinned in full: which expectation had no error, which error
        /// had no expectation, and the near miss that separates "the wrong rule fired" from "none did".
        /// </summary>
        [Fact]
        public void ShouldReport_NamesTheNearMiss_AndBothSidesOfTheDifference()
        {
            // Arrange
            var result = Result(("Vin", "Vin is required."), ("Cost", "Cost is required."));

            // Act
            var act = () => result.ShouldReport([new("Vin", "Vin is mandatory."), ExpectedError.Any("Mileage")]);

            // Assert
            act.Should().Throw<ValidationAssertionException>()
                .Which.Message.Should().Be(
                    """
                    Expected the validation result to report exactly 2 errors:
                      Vin      "Vin is mandatory."
                      Mileage  (any message)
                    but it reported 2 errors:
                      Vin   "Vin is required."
                      Cost  "Cost is required."

                    Not reported:
                      Vin      "Vin is mandatory."  (an error was reported under "Vin", but its message differs)
                      Mileage  (any message)        (nothing was reported under "Mileage")

                    Not expected:
                      Vin   "Vin is required."
                      Cost  "Cost is required."
                    """);
        }

        [Fact]
        public void ShouldReport_ForAnExpectedEmptyResult_Passes()
        {
            // Act
            var act = () => ValidationResult.Success.ShouldReport([]);

            // Assert
            act.Should().NotThrow();
        }

        /// <summary>
        /// A slip in the test is not a rule regression, and must not read like one.
        /// </summary>
        [Fact]
        public void ShouldReport_WithANullExpectation_ThrowsArgumentNullException()
        {
            // Arrange
            var result = Result(("Vin", "Vin is required."));

            // Act
            var act = () => result.ShouldReport([null!]);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ShouldReport_WithoutAnyExpectations_ThrowsArgumentNullException()
        {
            // Arrange
            var result = Result(("Vin", "Vin is required."));

            // Act
            var act = () => result.ShouldReport((IEnumerable<ExpectedError>)null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// A test which asserts that a validator throws must not swallow the failure of the assertion
        /// inside it.
        /// </summary>
        [Fact]
        public void ValidationAssertionException_IsNotAValidationException()
        {
            // Assert
            typeof(ValidationAssertionException).Should().NotBeAssignableTo<ValidationException>();
        }

        [Fact]
        public void ShouldReport_OnAValidationException_AssertsWhatItCarries()
        {
            // Arrange
            var exception = new ValidationException(Result(("Vin", "Vin is required.")));

            // Act
            var matching = () => exception.ShouldReport("Vin", "Vin is required.");
            var mismatching = () => exception.ShouldReport("Vin", "Vin is mandatory.");

            // Assert
            matching.Should().NotThrow();
            mismatching.Should().Throw<ValidationAssertionException>();
        }

        [Fact]
        public void ShouldReport_OnAnErrorsDictionary_CountsEachMessageOfACode()
        {
            // Arrange
            var errors = Result(("Vin", "first"), ("Vin", "second")).ToErrorsDictionary();

            // Act
            var bothNamed = () => errors.ShouldReport([new("Vin", "first"), new("Vin", "second")]);
            var onlyOneNamed = () => errors.ShouldReport("Vin", "first");

            // Assert
            bothNamed.Should().NotThrow();
            onlyOneNamed.Should().Throw<ValidationAssertionException>();
        }

        /// <summary>
        /// Whatever collection a caller's dictionary holds its messages in — the arrays of
        /// <see cref="ValidationResult.ToErrorsDictionary"/> and <see cref="ValidationException.Errors"/>,
        /// or a list of its own — goes through the same overload.
        /// </summary>
        [Fact]
        public void ShouldReport_OnAnErrorsDictionary_TakesEitherMessageCollection()
        {
            // Arrange
            IReadOnlyDictionary<string, string[]> arrays = Result(("Vin", "Vin is required.")).ToErrorsDictionary();
            IReadOnlyDictionary<string, IReadOnlyList<string>> lists =
                new Dictionary<string, IReadOnlyList<string>> { ["Vin"] = new List<string> { "Vin is required." } };

            // Act
            var fromArrays = () => arrays.ShouldReport("Vin", "Vin is required.");
            var fromLists = () => lists.ShouldReport("Vin", "Vin is required.");

            // Assert
            fromArrays.Should().NotThrow();
            fromLists.Should().NotThrow();
        }
    }
}
