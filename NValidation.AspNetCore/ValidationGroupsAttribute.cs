namespace NValidation.AspNetCore
{
    /// <summary>
    /// Selects the rule groups <see cref="ValidationActionFilter"/> runs for a payload:
    /// <c>[ValidationGroups("Create")]</c> on a parameter, an action or a controller, or
    /// <c>[ValidationGroups(All = true)]</c> for every group a validator declares.
    /// </summary>
    /// <remarks>
    /// The nearest one decides: the parameter's own, then the action's, then the controller's. A payload
    /// with none is validated as a plain call would be, so the chains in no group run and no other does.
    /// Only a controller action's parameter carries an attribute of its own; anywhere else the action's
    /// and the controller's are what is read.
    /// </remarks>
    /// <example>
    /// <code>
    /// [HttpPost("")]
    /// public ActionResult&lt;string&gt; Create([ValidationGroups("Create")] Car car)
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Parameter)]
    public sealed class ValidationGroupsAttribute : Attribute
    {
        private readonly string[] groups;

        /// <summary>
        /// Selects the named groups, or none where nothing is named, which is what
        /// <c>[ValidationGroups(All = true)]</c> is written for.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="groups"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">A name is <c>null</c>, empty or whitespace.</exception>
        public ValidationGroupsAttribute(params string[] groups)
        {
            ArgumentNullException.ThrowIfNull(groups);

            foreach (var group in groups)
            {
                if (string.IsNullOrWhiteSpace(group))
                {
                    throw new ArgumentException(
                        "A group name has to say something, so it cannot be empty.", nameof(groups));
                }
            }

            this.groups = groups;
        }

        /// <summary>
        /// Selects every group a validator declares, whatever it is called, and outranks any name given.
        /// </summary>
        public bool All { get; init; }

        /// <summary>
        /// What this selects: <see cref="ValidationGroups.All"/>, the named groups, or
        /// <see cref="ValidationGroups.None"/> where the attribute names nothing.
        /// </summary>
        public ValidationGroups Groups
        {
            get
            {
                // Read rather than stored, because All is an init property assigned after the constructor
                // has run.
                if (this.All)
                {
                    return ValidationGroups.All;
                }

                return this.groups.Length == 0 ? ValidationGroups.None : new ValidationGroups(this.groups);
            }
        }
    }
}
