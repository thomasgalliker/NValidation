namespace NValidation.TestData
{
    /// <summary>
    /// Optional equipment, combinable — so a value which is no declared member on its own is still
    /// legitimate. This is what tells <c>IsInEnum</c> apart from a plain enum.
    /// </summary>
    [Flags]
    public enum CarEquipment
    {
        None = 0,
        AirConditioning = 1,
        Navigation = 2,
        TowBar = 4,
        SunRoof = 8,
    }
}
