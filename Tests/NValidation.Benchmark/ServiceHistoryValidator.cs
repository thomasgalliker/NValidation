using NValidation.TestData;
using NValidation.TestData.Validators;

namespace NValidation.Benchmark
{
    /// <summary>
    /// Nothing but the element rules, so the measurement is the per-entry cost and not a collection
    /// rule rejecting the whole thing.
    /// </summary>
    /// <remarks>
    /// <see cref="CarValidator"/> caps the history, and a chain stops at its first failure, so measuring
    /// through it would report a collection over the cap as cheap — it never reaches the entries.
    /// </remarks>
    internal sealed class ServiceHistoryValidator : Validator<Car>
    {
        public ServiceHistoryValidator()
        {
            this.Property(c => c.ServiceHistory).ForEach(new ServiceRecordValidator());
        }
    }
}
