using System.Reflection;

namespace NValidation.Internals
{
    internal static class ValidatorTypeInfo
    {
        public static IEnumerable<Type> GetValidatedTypes(Type validatorType)
        {
            ArgumentNullException.ThrowIfNull(validatorType);

            // Nothing a scan could construct: still open on a type parameter, or abstract — which is
            // also what excludes an interface, because the CLI requires an interface definition to carry
            // the abstract flag. A clause of its own for IsInterface would never decide anything.
            if (validatorType.IsAbstract || validatorType.ContainsGenericParameters)
            {
                return [];
            }

            return validatorType
                .GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>));
        }

        public static IEnumerable<Type> GetValidatorTypes(Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            return GetLoadableTypes(assembly.GetTypes).Where(type => GetValidatedTypes(type).Any());
        }

        /// <summary>
        /// The types that loaded, where an assembly references something that was not deployed. Failing
        /// the whole scan — and with it startup — over a type nobody asked about would be the wrong trade.
        /// </summary>
        internal static IEnumerable<Type> GetLoadableTypes(Func<Type[]> getTypes)
        {
            try
            {
                return getTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null)!;
            }
        }
    }
}
