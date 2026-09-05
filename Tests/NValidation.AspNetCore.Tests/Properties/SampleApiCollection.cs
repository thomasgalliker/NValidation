using NValidation.AspNetCore.Tests;
using Xunit;

/// <summary>
/// One host is booted for every test that needs the sample application, so they share the fixture and
/// run in sequence.
/// </summary>
[CollectionDefinition(Collections.SampleApi, DisableParallelization = true)]
public class SampleApiCollection : ICollectionFixture<SampleApiTestFixture>;
