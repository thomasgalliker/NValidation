namespace NValidation.TestData.Validators
{
    internal sealed class ConditionIsInEnumValidator : Validator<Car>
    {
        public ConditionIsInEnumValidator()
        {
            this.Property(c => c.Condition).IsInEnum();
        }
    }

    /// <summary>
    /// The <see cref="FlagsAttribute"/> counterpart: a combination of members is legitimate even though
    /// it is not itself a declared member.
    /// </summary>
    internal sealed class EquipmentIsInEnumValidator : Validator<Car>
    {
        public EquipmentIsInEnumValidator()
        {
            this.Property(c => c.Equipment).IsInEnum();
        }
    }

    internal sealed class GearDirectionIsInEnumValidator : Validator<Car>
    {
        public GearDirectionIsInEnumValidator()
        {
            this.Property(c => c.GearDirection).IsInEnum();
        }
    }
}
