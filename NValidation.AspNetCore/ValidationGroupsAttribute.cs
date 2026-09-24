namespace NValidation.AspNetCore
{
    /// <summary>
    /// Selects the rule groups <see cref="ValidationActionFilter"/> runs for a payload:
    /// <c>[ValidationGroups("Create")]</c> on a parameter, an action or a controller,
    /// <c>[ValidationGroups("Listing", Only = true)]</c> for those groups without the default group, or
    /// <c>[ValidationGroups(All = true)]</c> for every group a validator declares.
    /// </summary>
    /// <remarks>
    /// The nearest one decides: the parameter's own, then the action's, then the controller's. A payload
    /// with none is validated as a plain call would be, so the default group runs and no other does.
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
        /// Runs the named groups without the default group, as <see cref="ValidationGroups.Only"/> does.
        /// </summary>
        /// <remarks>
        /// The validator of every payload the attribute reaches must declare one of the groups, or the
        /// request fails rather than check nothing — which matters most on a controller with several
        /// actions.
        /// </remarks>
        public bool Only { get; init; }

        /// <summary>
        /// What this selects: <see cref="ValidationGroups.All"/>, the named groups with or without the
        /// default group, or <see cref="ValidationGroups.None"/> where the attribute names nothing.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// <see cref="Only"/> is set together with <see cref="All"/>, or without a group to run.
        /// </exception>
        public ValidationGroups Groups
        {
            get
            {
                // Read rather than stored, because All and Only are init properties assigned after the
                // constructor has run.
                if (this.All)
                {
                    return this.Only
                        ? throw new InvalidOperationException(
                            "[ValidationGroups] cannot run every group and only some of them: set All or Only, not both.")
                        : ValidationGroups.All;
                }

                if (this.Only)
                {
                    return this.groups.Length == 0
                        ? throw new InvalidOperationException(
                            "[ValidationGroups(Only = true)] has to name the groups it runs.")
                        : ValidationGroups.Only(this.groups);
                }

                return this.groups.Length == 0 ? ValidationGroups.None : new ValidationGroups(this.groups);
            }
        }
    }
}
