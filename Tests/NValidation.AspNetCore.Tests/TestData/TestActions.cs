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

        public void CreateSkippedParameter([SkipNValidation("Validated by the action itself.")] Car car)
        {
        }

        [SkipNValidation("Validated by the action itself.")]
        public void CreateSkippedAction(Car car)
        {
        }

        public void CreateSkippedParameterWithoutAReason([SkipNValidation] Car car)
        {
        }

        public void ImportSkippedParameter([SkipNValidation("Reports failures per row, not as a 400.")] CarImport carImport)
        {
        }

        public void CreateInTheCreateGroup([ValidationGroups("Create")] Car car)
        {
        }

        [ValidationGroups("Create")]
        public void CreateGroupedAction(Car car)
        {
        }

        [ValidationGroups("Update")]
        public void CreateInAnotherGroup(Car car)
        {
        }

        [ValidationGroups(All = true)]
        public void CreateInEveryGroup(Car car)
        {
        }

        [ValidationGroups("Listing", Only = true)]
        public void CheckListingAlone(Car car)
        {
        }

        [ValidationGroups("Listng", Only = true)]
        public void CheckAMistypedGroupAlone(Car car)
        {
        }
    }
}
