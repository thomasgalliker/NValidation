namespace NValidation.AspNetCore.Tests
{
    /// <summary>
    /// What the attribute accepts and what it selects. A name has to say something, and asking for every
    /// group is a decision of its own rather than a name nobody may use.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class ValidationGroupsAttributeTests
    {
        [Fact]
        public void Constructor_WithNames_SelectsThoseGroups()
        {
            // Act
            var attribute = new ValidationGroupsAttribute("Create", "Update");

            // Assert
            attribute.Groups.Names.Should().Equal("Create", "Update");
            attribute.All.Should().BeFalse();
        }

        [Fact]
        public void Constructor_WithoutAName_SelectsNoGroup()
        {
            // Act
            var attribute = new ValidationGroupsAttribute();

            // Assert
            attribute.Groups.Should().BeSameAs(ValidationGroups.None);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Constructor_WithABlankName_Throws(string? group)
        {
            // Act
            var act = () => new ValidationGroupsAttribute("Create", group!);

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("groups");
        }

        [Fact]
        public void Constructor_WithoutAnArray_Throws()
        {
            // Act
            var act = () => new ValidationGroupsAttribute(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("groups");
        }

        [Fact]
        public void All_SelectsEveryGroup()
        {
            // Act
            var attribute = new ValidationGroupsAttribute { All = true };

            // Assert
            attribute.Groups.Should().BeSameAs(ValidationGroups.All);
        }

        [Fact]
        public void All_OutranksTheNamesGiven()
        {
            // Act
            var attribute = new ValidationGroupsAttribute("Create") { All = true };

            // Assert
            attribute.Groups.Should().BeSameAs(ValidationGroups.All);
        }

        [Fact]
        public void Only_SelectsTheNamedGroupsWithoutTheDefaultGroup()
        {
            // Act
            var attribute = new ValidationGroupsAttribute("Listing") { Only = true };

            // Assert
            attribute.Groups.Names.Should().Equal("Listing");
            attribute.Groups.IncludesDefault.Should().BeFalse();
        }

        [Fact]
        public void Only_WithoutAName_Throws()
        {
            // Arrange
            var attribute = new ValidationGroupsAttribute { Only = true };

            // Act
            var act = () => attribute.Groups;

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("*has to name the groups it runs*");
        }

        [Fact]
        public void Only_TogetherWithAll_Throws()
        {
            // Arrange
            var attribute = new ValidationGroupsAttribute("Listing") { All = true, Only = true };

            // Act
            var act = () => attribute.Groups;

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("*All or Only, not both*");
        }
    }
}
