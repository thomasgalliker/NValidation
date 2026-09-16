namespace NValidation.Tests
{
    /// <summary>
    /// Records a boundary this library keeps on purpose: what a composer declared <em>for itself</em> is
    /// not a default for the validators it composes. Only what the run was asked for is — through the
    /// options passed to a call, or through <see cref="NValidationOptions.Default"/>.
    /// </summary>
    /// <remarks>
    /// Worth pinning precisely because those two routes make the common case look like full propagation.
    /// Without this test, someone "fixing" the remaining gap would not see what they were changing. The
    /// counterpart — the same composition, reached through per-call options — is
    /// <c>NValidationOptionsTests.ValidateAsync_WithOptions_ReachANestedValidator</c>.
    /// </remarks>
    [Trait(Traits.Category, Traits.UnitTests)]
    [Collection(Collections.ValidationDefaults)]
    public class ValidationBehaviorPropagationTests : IDisposable
    {
        // Depends on NValidationOptions.Default being untouched, so it brackets it like every other test
        // which reads them.
        public ValidationBehaviorPropagationTests()
        {
            NValidationOptions.Default.Reset();
        }

        public void Dispose()
        {
            NValidationOptions.Default.Reset();
        }

        [Fact]
        public async Task ValidateAsync_WhatAComposerDeclaredForItself_DoesNotReachANestedValidator()
        {
            // Arrange
            var validator = new ComposingValidator(new TwoRuleValidator());
            validator.ValidationBehaviors.Property = ValidationBehavior.All;

            // Act
            var result = await validator.ValidateAsync(new CarModel { Manufacturer = new Manufacturer() });

            // Assert — the nested validator kept the built-in default of one message per property
            result.ShouldReport("Manufacturer.Name", "Name must be Aurora.");
        }

        private sealed class TwoRuleValidator : Validator<Manufacturer>
        {
            public TwoRuleValidator()
            {
                this.Property(m => m.Name)
                    .Must(name => name == "Aurora", "Name must be Aurora.")
                    .Must(name => name == "Northgate", "Name must be spelled out.");
            }
        }

        private sealed class ComposingValidator : Validator<CarModel>
        {
            public ComposingValidator(IValidator<Manufacturer> manufacturerValidator)
            {
                this.Property(m => m.Manufacturer).SetValidator(manufacturerValidator);
            }
        }
    }
}
