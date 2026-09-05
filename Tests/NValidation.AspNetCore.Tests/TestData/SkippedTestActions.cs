namespace NValidation.AspNetCore.Tests.TestData
{
    /// <summary>
    /// The controller-level exclusion: every action of this class is skipped, so the filter is proven to
    /// read the attribute from the declaring type and not only from the action or the parameter.
    /// </summary>
    [SkipValidation("The whole controller answers in a legacy error shape.")]
    public class SkippedTestActions
    {
        public void Create(Car car)
        {
        }
    }
}
