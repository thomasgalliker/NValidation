using NValidation.TestData;
using NValidation.TestData.Validators;

namespace NValidation.ConsoleApp;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var carValidator = new CarValidator(new CarModelValidator(new ManufacturerValidator()), new ServiceRecordValidator());
        carValidator.Messages = new DefaultValidationMessageProvider();
        carValidator.ValidationBehaviors.Class = ValidationBehavior.All;
        carValidator.ValidationBehaviors.Property = ValidationBehavior.StopAtFirstError;

        var car = Cars.Car();
        car.Vin = "invalid-test-vin";
        car.Mileage = -1000;
        car.PurchasePrice = -1000m;

        var result = await carValidator.ValidateAsync(car);
        Console.WriteLine(ObjectDumper.Dump(result));
    }
}