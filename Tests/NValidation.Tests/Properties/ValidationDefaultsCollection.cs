using Xunit;

/// <summary>
/// Tests which configure <see cref="NValidation.NValidationOptions.Default"/> cannot run next to
/// anything else, because the defaults are process-wide and almost every other test in this assembly
/// asserts the built-in English a run produces.
/// </summary>
/// <remarks>
/// Serializing them is only half of it: the defaults freeze the first time anything validates, so a
/// test in this collection also has to call <c>Reset()</c> in its constructor — an earlier test in some
/// other collection has already frozen them. A test which passes its own options needs neither.
/// </remarks>
[CollectionDefinition(Collections.ValidationDefaults, DisableParallelization = true)]
public class ValidationDefaultsCollection;
