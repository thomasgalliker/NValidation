namespace NValidation.Internals
{
    /// <summary>
    /// The cast behind the untyped IValidator entry points. Lives outside IValidator because that interface
    /// is contravariant and a member of it may not return T.
    /// </summary>
    internal static class Payload
    {
        public static T Cast<T>(object instance, Type validatorType)
        {
            ArgumentNullException.ThrowIfNull(instance);

            if (instance is not T typed)
            {
                throw new InvalidCastException(
                    $"{validatorType.GetFormattedFullName()} validates {typeof(T).GetFormattedFullName()}, " +
                    $"so it cannot validate an instance of {instance.GetType().GetFormattedFullName()}.");
            }

            return typed;
        }
    }
}
