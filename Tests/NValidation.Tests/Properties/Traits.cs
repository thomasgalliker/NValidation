/// <summary>
/// Test category names. The build pipeline selects tests by category, so a test without a
/// <see cref="Traits.Category"/> trait is never executed.
/// </summary>
public static class Traits
{
    public const string Category = "Category";
    public const string UnitTests = "UnitTests";
    public const string IntegrationTests = "IntegrationTests";
}
