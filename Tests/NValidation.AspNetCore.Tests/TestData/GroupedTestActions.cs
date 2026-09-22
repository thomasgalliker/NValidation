namespace NValidation.AspNetCore.Tests.TestData
{
    /// <summary>
    /// A controller whose every action selects a rule group, so what an action or a parameter says of its
    /// own can be told from what it inherited.
    /// </summary>
    [ValidationGroups("Create")]
    public class GroupedTestActions
    {
        public void Create(Car car)
        {
        }

        [ValidationGroups("Update")]
        public void Update(Car car)
        {
        }

        public void UpdateByParameter([ValidationGroups("Update")] Car car)
        {
        }
    }
}
