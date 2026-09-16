using NValidation.TestData;
using NValidation.TestData.Validators;

namespace NValidation.ConsoleApp;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        // Configured once for the process, before anything validates. Every validator which has not
        // said otherwise for itself picks these up — including the three nested ones constructed
        // below, which no assignment on carValidator could ever have reached.
        NValidationOptions.Default.MessageProvider = DefaultValidationMessageProvider.Instance;
        NValidationOptions.Default.ValidationBehaviors.Class = ValidationBehavior.All;
        NValidationOptions.Default.ValidationBehaviors.Property = ValidationBehavior.StopAtFirstError;

        var carValidator = new CarValidator(new CarModelValidator(new ManufacturerValidator()), new ServiceRecordValidator());

        var car = Cars.Car();
        car.Vin = "invalid-test-vin";
        car.Mileage = -1000;
        car.PurchasePrice = -1000m;

        var result = await carValidator.ValidateAsync(car);
        Console.WriteLine(ObjectDumper.Dump(result));
    }
}
