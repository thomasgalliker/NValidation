using NValidation.TestData;
using NValidation.TestData.Validators;

namespace NValidation.ConsoleApp;

internal static class Program
{
    public static async Task Main()
    {
        // Configured NValidation once for the process, before any validation is run.
        NValidationOptions.Default = new NValidationOptions
        {
            MessageProvider = DefaultValidationMessageProvider.Instance,
            ValidationBehaviors = new()
            {
                Class = ValidationBehavior.All,
                Property = ValidationBehavior.StopAtFirstError,
            },
        };

        // Create instance of validator.
        var carValidator = new CarValidator(new CarModelValidator(new ManufacturerValidator()), new ServiceRecordValidator());

        // Create instance of test object.
        var car = Cars.Car();
        car.Vin = "invalid-test-vin";
        car.Mileage = -1000;
        car.PurchasePrice = -1000m;

        // Run validation for car.
        var result = await carValidator.ValidateAsync(car);

        // Print the ValidationResult with its list of ValidationError to the console.
        Console.WriteLine(ObjectDumper.Dump(result));

        // The same car judged as one being taken in: the chains in no group run again, and the chain
        // CarValidator declares for the Create group runs as well.
        var createResult = await carValidator.ValidateAsync(car, new NValidationOptions { ValidationGroups = CarValidator.CreateGroup });

        Console.WriteLine(ObjectDumper.Dump(createResult));
    }
}