using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.Testing;
using Superdev.AspNetCore.Testing;

namespace NValidation.AspNetCore.Tests
{
    /// <summary>
    /// Hosts the sample application, so the filter is exercised through a real request pipeline: model
    /// binding, the filter, and the host's problem details response.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class LoggingSampleApiTestFixture : SampleApiTestFixture
    {
        protected override MissingValidatorBehavior MissingValidatorBehavior => MissingValidatorBehavior.Log;
    }
}
