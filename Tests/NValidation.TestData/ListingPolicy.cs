namespace NValidation.TestData
{
    /// <summary>
    /// What the market a car is offered in asks of it: something the car cannot answer about itself, so a
    /// listing check hands it to the validation as <see cref="ValidationData"/>.
    /// </summary>
    public sealed record ListingPolicy(int MaximumMileage, bool RequiresServiceHistory);
}
