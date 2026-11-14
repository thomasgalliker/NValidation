using Xunit;

/// <summary>
/// Tests which assign <see cref="NValidation.NValidationOptions.Default"/> cannot run next to anything
/// else, because it is process-wide and other tests in this assembly assert the built-in English.
/// </summary>
/// <remarks>
/// They keep the original instance and put it back afterwards; the collection is what stops a test
/// reading the defaults while another has its own instance installed.
/// </remarks>
[CollectionDefinition(Collections.ValidationDefaults, DisableParallelization = true)]
public class ValidationDefaultsCollection;
