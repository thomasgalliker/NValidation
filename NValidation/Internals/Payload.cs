namespace NValidation.Internals
{
    /// <summary>
    /// The cast behind the untyped <see cref="IValidator"/> entry points. Lives outside
    /// <see cref="IValidator{T}"/> because that interface is contravariant and a member of it may not
    /// return <c>T</c>.
    /// </summary>
    internal static class Payload
    {
        /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidCastException"><paramref name="instance"/> is not a <typeparamref name="T"/>.</exception>
        public static T Cast<T>(object instance, Type validatorType)
        {
            ArgumentNullException.ThrowIfNull(instance);

            if (instance is not T typed)
            {
                throw new InvalidCastException(
                    $"{validatorType} validates {typeof(T)}, so it cannot validate an instance of {instance.GetType()}.");
            }

            return typed;
        }
    }
}
