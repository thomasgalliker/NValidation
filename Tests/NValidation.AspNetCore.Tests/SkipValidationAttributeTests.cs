namespace NValidation.AspNetCore.Tests
{
    /// <summary>
    /// What the attribute accepts. A reason is optional, but a stated one has to say something: an empty
    /// string is a reason someone started writing and never finished.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class SkipValidationAttributeTests
    {
        [Fact]
        public void Constructor_WithoutAReason_LeavesTheReasonUnstated()
        {
            // Act
            var attribute = new SkipValidationAttribute();

            // Assert
            attribute.Reason.Should().BeNull();
        }

        [Fact]
        public void Constructor_WithAReason_KeepsIt()
        {
            // Act
            var attribute = new SkipValidationAttribute("Reports failures per row, not as a 400.");

            // Assert
            attribute.Reason.Should().Be("Reports failures per row, not as a 400.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Constructor_WithAnEmptyReason_Throws(string? reason)
        {
            // Act
            var act = () => new SkipValidationAttribute(reason!);

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("reason");
        }
    }
}
