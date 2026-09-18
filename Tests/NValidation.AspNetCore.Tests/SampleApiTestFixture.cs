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
    public class SampleApiTestFixture : TestFixture<Program>
    {
        /// <summary>
        /// Declared as a property rather than taken through the constructor, because
        /// <see cref="Initialize"/> runs before a derived constructor body would have assigned a field.
        /// </summary>
        protected virtual MissingValidatorBehavior MissingValidatorBehavior => MissingValidatorBehavior.Ignore;

        protected override void Initialize()
        {
            // Wins over the sample's own appsettings.json, so each fixture pins one behaviour.
            this.PostConfigure<ValidationFilterOptions>(o => o.MissingValidatorBehavior = this.MissingValidatorBehavior);
        }

        public HttpClient GetHttpClient()
        {
            return this.GetOrCreateFactory().CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
        }
    }
}
