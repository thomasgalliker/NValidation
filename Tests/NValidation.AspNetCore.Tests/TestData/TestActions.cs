namespace NValidation.AspNetCore.Tests.TestData
{
    /// <summary>
    /// The action shapes the filter is exercised against. Only their signatures and attributes matter:
    /// the filter reads them off the action descriptor and never invokes them.
    /// </summary>
    public class TestActions
    {
        public void Create(Car car)
        {
        }

        public void CreateWithId(int carId, Car car)
        {
        }

        public void Import(CarImport carImport)
        {
        }

        public void CreateWithManufacturer(Car car, Manufacturer manufacturer)
        {
        }

        public void CreateSkippedParameter([SkipValidation("Validated by the action itself.")] Car car)
        {
        }

        [SkipValidation("Validated by the action itself.")]
        public void CreateSkippedAction(Car car)
        {
        }

        public void CreateSkippedParameterWithoutAReason([SkipValidation] Car car)
        {
        }

        public void ImportSkippedParameter([SkipValidation("Reports failures per row, not as a 400.")] CarImport carImport)
        {
        }
    }
}
