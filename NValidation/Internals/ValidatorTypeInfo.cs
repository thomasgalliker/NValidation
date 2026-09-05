using System.Reflection;

namespace NValidation.Internals
{
    /// <summary>
    /// Finds what a validator type validates, so a scan can register it under the right service type.
    /// </summary>
    internal static class ValidatorTypeInfo
    {
        /// <summary>
        /// The closed <see cref="IValidator{T}"/> interfaces a type implements. A validator may serve
        /// more than one, and an open generic validator serves none until it is closed.
        /// </summary>
        public static IEnumerable<Type> GetValidatedTypes(Type validatorType)
        {
            ArgumentNullException.ThrowIfNull(validatorType);

            if (validatorType.IsAbstract || validatorType.IsInterface || validatorType.ContainsGenericParameters)
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

            return GetLoadableTypes(assembly).Where(type => GetValidatedTypes(type).Any());
        }

        /// <remarks>
        /// An assembly which references something that was not deployed still loads most of its types,
        /// and the validators are usually among them. Failing the whole scan — and with it the
        /// application's startup — over a type nobody asked about would be the wrong trade.
        /// </remarks>
        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            return GetLoadableTypes(assembly.GetTypes);
        }

        /// <summary>
        /// Split from the assembly so the recovery path can be exercised directly: arranging an
        /// assembly which half-loads is not something a test can do honestly.
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
