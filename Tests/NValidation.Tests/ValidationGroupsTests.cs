namespace NValidation.Tests
{
    /// <summary>
    /// What a selection accepts and what it selects. The three shapes — none, all, and a list of names —
    /// are what a validation is asked for, and a name is compared for what it is rather than for how it
    /// is spelled.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class ValidationGroupsTests
    {
        [Fact]
        public void Constructor_WithNames_KeepsThem()
        {
            // Act
            var groups = new ValidationGroups("Create", "Update");

            // Assert
            groups.Names.Should().Equal("Create", "Update");
            groups.IncludesAll.Should().BeFalse();
        }

        [Fact]
        public void Constructor_WithoutAName_SelectsNothing()
        {
            // Act
            var groups = new ValidationGroups();

            // Assert
            groups.Names.Should().BeEmpty();
            groups.IncludesAll.Should().BeFalse();
            groups.Selects(["Create"]).Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Constructor_WithABlankName_Throws(string? group)
        {
            // Act
            var act = () => new ValidationGroups("Create", group!);

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("groups");
        }

        [Fact]
        public void None_SelectsNoGroupedChain()
        {
            // Assert
            ValidationGroups.None.Selects(["Create"]).Should().BeFalse();
            ValidationGroups.None.Names.Should().BeEmpty();
            ValidationGroups.None.IncludesAll.Should().BeFalse();
        }

        [Fact]
        public void All_SelectsEveryGroupedChain()
        {
            // Assert
            ValidationGroups.All.Selects(["Anything"]).Should().BeTrue();
            ValidationGroups.All.Names.Should().BeEmpty();
            ValidationGroups.All.IncludesAll.Should().BeTrue();
        }

        [Fact]
        public void Selects_WhenAChainIsInANamedGroup_ReturnsTrue()
        {
            // Arrange
            var groups = new ValidationGroups("Update", "Create");

            // Act
            var selects = groups.Selects(["Delete", "Create"]);

            // Assert
            selects.Should().BeTrue();
        }

        [Fact]
        public void Selects_WhenAChainIsInNoNamedGroup_ReturnsFalse()
        {
            // Arrange
            var groups = new ValidationGroups("Update");

            // Act
            var selects = groups.Selects(["Create"]);

            // Assert
            selects.Should().BeFalse();
        }

        /// <summary>
        /// A group name is a token an application chose, not prose, so it is compared for what it is.
        /// </summary>
        [Fact]
        public void Selects_ComparesNamesOrdinally()
        {
            // Arrange
            var groups = new ValidationGroups("create");

            // Act
            var selects = groups.Selects(["Create"]);

            // Assert
            selects.Should().BeFalse();
        }

        [Fact]
        public void Selects_WithARepeatedName_StillSelectsIt()
        {
            // Arrange
            var groups = new ValidationGroups("Create", "Create");

            // Act
            var selects = groups.Selects(["Create"]);

            // Assert
            selects.Should().BeTrue();
        }

        [Fact]
        public void ImplicitOperator_FromAName_SelectsThatGroup()
        {
            // Act
            ValidationGroups groups = "Create";

            // Assert
            groups.Names.Should().Equal("Create");
            groups.Selects(["Create"]).Should().BeTrue();
        }

        [Fact]
        public void ImplicitOperator_FromANullName_SelectsNothing()
        {
            // Act
            ValidationGroups groups = (string?)null;

            // Assert
            groups.Should().BeSameAs(ValidationGroups.None);
        }

        [Fact]
        public void ImplicitOperator_FromAnArrayOfNames_SelectsThoseGroups()
        {
            // Arrange
            var names = new[] { "Create", "Update" };

            // Act
            ValidationGroups groups = names;

            // Assert
            groups.Names.Should().Equal("Create", "Update");
        }

        [Fact]
        public void ImplicitOperator_FromANullArray_SelectsNothing()
        {
            // Act
            ValidationGroups groups = (string[]?)null;

            // Assert
            groups.Should().BeSameAs(ValidationGroups.None);
        }

        [Fact]
        public void CollectionExpression_WithNames_SelectsThoseGroups()
        {
            // Act
            ValidationGroups groups = ["Create", "Update"];

            // Assert
            groups.Names.Should().Equal("Create", "Update");
        }

        [Fact]
        public void CollectionExpression_WithoutAName_SelectsNothing()
        {
            // Act
            ValidationGroups groups = [];

            // Assert
            groups.Should().BeSameAs(ValidationGroups.None);
        }

        [Fact]
        public void ToString_WithNames_JoinsThem()
        {
            // Act
            var text = new ValidationGroups("Create", "Update").ToString();

            // Assert
            text.Should().Be("Create, Update");
        }

        [Fact]
        public void ToString_ForNone_SaysNone()
        {
            // Assert
            ValidationGroups.None.ToString().Should().Be("None");
        }

        [Fact]
        public void ToString_ForAll_SaysAll()
        {
            // Assert
            ValidationGroups.All.ToString().Should().Be("All");
        }

        [Fact]
        public void ToString_ForOnly_SaysOnly()
        {
            // Act
            var text = ValidationGroups.Only("Listing", "Pricing").ToString();

            // Assert
            text.Should().Be("Only: Listing, Pricing");
        }

        [Fact]
        public void Only_WithNames_LeavesTheDefaultGroupOut()
        {
            // Act
            var groups = ValidationGroups.Only("Listing");

            // Assert
            groups.Names.Should().Equal("Listing");
            groups.IncludesDefault.Should().BeFalse();
            groups.IncludesAll.Should().BeFalse();
            groups.Selects(["Listing"]).Should().BeTrue();
        }

        [Fact]
        public void Only_NamingTheDefaultGroupBesideAnother_IsTheAdditiveSelection()
        {
            // Act
            var groups = ValidationGroups.Only(ValidationGroups.DefaultGroup, "Listing");

            // Assert
            groups.Names.Should().Equal("Listing");
            groups.IncludesDefault.Should().BeTrue();
        }

        [Fact]
        public void Only_NamingTheDefaultGroupAlone_IsNone()
        {
            // Act
            var groups = ValidationGroups.Only(ValidationGroups.DefaultGroup);

            // Assert
            groups.Should().BeSameAs(ValidationGroups.None);
        }

        [Fact]
        public void Only_WithoutAName_Throws()
        {
            // Act
            var act = () => ValidationGroups.Only();

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("groups");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Only_WithABlankName_Throws(string? group)
        {
            // Act
            var act = () => ValidationGroups.Only("Listing", group!);

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("groups");
        }

        [Fact]
        public void Constructor_NamingTheDefaultGroup_LeavesItOutOfTheNames()
        {
            // Act
            var groups = new ValidationGroups(ValidationGroups.DefaultGroup, "Create");

            // Assert
            groups.Names.Should().Equal("Create");
            groups.IncludesDefault.Should().BeTrue();
        }

        [Fact]
        public void IncludesDefault_ForEveryAdditiveSelection_IsTrue()
        {
            // Assert
            ValidationGroups.None.IncludesDefault.Should().BeTrue();
            ValidationGroups.All.IncludesDefault.Should().BeTrue();
            new ValidationGroups("Create").IncludesDefault.Should().BeTrue();
        }

        [Fact]
        public void Additive_ForOnly_KeepsTheNamesAndAddsTheDefaultGroup()
        {
            // Arrange
            var groups = ValidationGroups.Only("Listing");

            // Act
            var additive = groups.Additive;

            // Assert
            additive.Names.Should().Equal("Listing");
            additive.IncludesDefault.Should().BeTrue();
            groups.Additive.Should().BeSameAs(additive);
        }

        [Fact]
        public void Additive_ForAnAdditiveSelection_IsTheSelectionItself()
        {
            // Arrange
            var groups = new ValidationGroups("Create");

            // Act
            var additive = groups.Additive;

            // Assert
            additive.Should().BeSameAs(groups);
        }

        [Fact]
        public void Selects_WithoutGroups_ReturnsFalse()
        {
            // Act
            var selects = ValidationGroups.All.Selects(null);

            // Assert
            selects.Should().BeFalse();
        }
    }
}
