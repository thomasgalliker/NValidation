using System.Runtime.CompilerServices;

namespace NValidation.Internals
{
    internal readonly record struct ValidationRun(
        InheritedSettings Inherited,
        ElementScope? Scope,
        CancellationToken CancellationToken)
    {
        /// <summary>
        /// This run as a chain reported under <paramref name="propertyName"/> hands it to what it composes:
        /// where the call is limited to some properties, limited to what of them lies below that chain.
        /// </summary>
        public ValidationRun Below(string propertyName)
        {
            return this.Inherited.Inputs.Properties is { } properties ? this.Narrowed(properties, propertyName) : this;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ValidationRun Narrowed(ValidationProperties properties, string propertyName)
        {
            var inputs = this.Inherited.Inputs;
            var below = inputs.WithProperties(properties.Below(propertyName));

            return this with { Inherited = this.Inherited.WithInputs(below) };
        }
    }
}
