namespace NValidation.TestData
{
    /// <summary>
    /// Declares a negative member, which is legal and not unusual — reverse is below neutral. Kept as
    /// its own enum because an enum's underlying values decide how <c>IsInEnum</c> reads it.
    /// </summary>
    public enum GearDirection
    {
        Reverse = -1,
        Neutral = 0,
        Forward = 1,
    }
}
