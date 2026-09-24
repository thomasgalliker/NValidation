using System.Collections;
using System.Runtime.CompilerServices;
using NValidation.Internals;

namespace NValidation
{
    /// <summary>
    /// Which rule groups a validation runs: <c>ValidationGroups.None</c> for the default group alone,
    /// names to run on top of it, <see cref="Only"/> names to run without it, or
    /// <see cref="All"/>.
    /// </summary>
    /// <remarks>
    /// Immutable, and converted from what a caller would otherwise write by hand: a name, an array of
    /// names, or a collection expression — <c>ValidationGroups = "Create"</c> and
    /// <c>ValidationGroups = ["Create", "Update"]</c> both compile, and both keep the default group. Names
    /// are compared ordinally. Carried by <see cref="NValidationOptions.ValidationGroups"/> and resolved per
    /// call, so a validator never selects groups for itself.
    /// </remarks>
    [CollectionBuilder(typeof(ValidationGroups), nameof(Create))]
    public sealed class ValidationGroups : IEnumerable<string>
    {
        /// <summary>
        /// The group every chain declared without a group is in. Named beside another —
        /// <c>WithGroup(ValidationGroups.DefaultGroup, "Listing")</c> — it keeps a chain running in every
        /// validation while <see cref="Only"/> can still select it by the other name.
        /// </summary>
        public const string DefaultGroup = "Default";

        private readonly string[] names;

        private ValidationGroups? additive;

        private ValidationGroups(string[] names, bool includesDefault, bool includesAll)
        {
            this.names = names;
            this.IncludesDefault = includesDefault;
            this.IncludesAll = includesAll;
        }

        /// <summary>
        /// Selects the chains in the named groups on top of the default group; naming none is
        /// <see cref="None"/>.
        /// </summary>
        /// <exception cref="ArgumentException">A name is <c>null</c>, empty or whitespace.</exception>
        public ValidationGroups(params ReadOnlySpan<string> groups)
            : this(GroupNames.WithoutDefault(GroupNames.Copy(groups, nameof(groups))), includesDefault: true, includesAll: false)
        {
        }

        /// <summary>
        /// Only the chains in the default group run. What a validation selects unless something asked for
        /// more.
        /// </summary>
        public static ValidationGroups None { get; } = new(GroupNames.Empty, includesDefault: true, includesAll: false);

        /// <summary>
        /// Every chain runs, whatever group it is in.
        /// </summary>
        public static ValidationGroups All { get; } = new(GroupNames.Empty, includesDefault: true, includesAll: true);

        /// <summary>
        /// The groups this selects by name, without the default group; empty for <see cref="None"/> and for
        /// <see cref="All"/>, which select by what they are rather than by naming anything.
        /// </summary>
        public IReadOnlyList<string> Names => this.names;

        /// <summary>
        /// Whether the chains in the default group run: <c>false</c> only for a selection made with
        /// <see cref="Only"/>.
        /// </summary>
        public bool IncludesDefault { get; }

        /// <summary>
        /// Whether every grouped chain is selected, whatever its group is called.
        /// </summary>
        public bool IncludesAll { get; }

        /// <summary>
        /// The additive form of an exclusive selection, which is what a validator that declares none of its
        /// groups is validated with; itself for every other selection. Built once, on first use.
        /// </summary>
        internal ValidationGroups Additive => this.IncludesDefault
            ? this
            : this.additive ??= new ValidationGroups(this.names, includesDefault: true, includesAll: false);

        /// <summary>
        /// Selects the chains in the named groups and nothing else, leaving the default group out:
        /// <c>ValidationGroups.Only("Listing")</c> checks what a listing needs without the rest of the
        /// object.
        /// </summary>
        /// <remarks>
        /// A validator this is passed to must declare at least one of the groups, or the call throws rather
        /// than check nothing. A validator it reaches through a chain — a nested one, the entries of a
        /// <c>ForEach</c> — runs its chains in these groups where it declares any, and is validated as a
        /// plain call would validate it where it declares none. Naming
        /// <see cref="DefaultGroup"/> among them gives the additive selection of the others.
        /// </remarks>
        /// <exception cref="ArgumentException">No group is named, or a name is empty or whitespace.</exception>
        public static ValidationGroups Only(params ReadOnlySpan<string> groups)
        {
            GroupNames.ThrowIfEmpty(groups, nameof(groups));

            var named = GroupNames.Copy(groups, nameof(groups));
            var withoutDefault = GroupNames.WithoutDefault(named);

            if (withoutDefault.Length == named.Length)
            {
                return new ValidationGroups(named, includesDefault: false, includesAll: false);
            }

            return withoutDefault.Length == 0
                ? None
                : new ValidationGroups(withoutDefault, includesDefault: true, includesAll: false);
        }

        /// <summary>
        /// Selects the chains in the named groups on top of the default group, for a collection
        /// expression: <c>ValidationGroups groups = ["Create", "Update"];</c>
        /// </summary>
        /// <inheritdoc cref="ValidationGroups(ReadOnlySpan{string})" path="/exception"/>
        public static ValidationGroups Create(ReadOnlySpan<string> groups)
        {
            return groups.Length == 0 ? None : new ValidationGroups(groups);
        }

        /// <summary>
        /// Selects one group on top of the default group, so a caller writing a single name does not have
        /// to say the type: <c>ValidationGroups = "Create"</c>.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="group"/> is empty or whitespace.</exception>
        public static implicit operator ValidationGroups(string? group)
        {
            return group is null ? None : new ValidationGroups(group);
        }

        /// <summary>
        /// Selects the groups an array names on top of the default group, for a caller which already holds
        /// one.
        /// </summary>
        /// <inheritdoc cref="ValidationGroups(ReadOnlySpan{string})" path="/exception"/>
        public static implicit operator ValidationGroups(string[]? groups)
        {
            return groups is null ? None : Create(groups);
        }

        /// <inheritdoc/>
        public IEnumerator<string> GetEnumerator()
        {
            return ((IEnumerable<string>)this.names).GetEnumerator();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (this.IncludesAll)
            {
                return "All";
            }

            if (!this.IncludesDefault)
            {
                return $"Only: {string.Join(", ", this.names)}";
            }

            return this.names.Length == 0 ? "None" : string.Join(", ", this.names);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Whether this runs the default group and nothing else, which is what a call without groups asks for.
        /// </summary>
        internal bool SelectsTheDefaultGroupAlone => this.IncludesDefault && !this.IncludesAll && this.names.Length == 0;

        /// <summary>
        /// Whether a validator walking its chains one by one is needed at all: where every chain runs
        /// anyway, the loop without a gate does the same work for less.
        /// </summary>
        internal bool RunsEveryChain(bool everyChainInDefault)
        {
            return this.IncludesAll || (this.IncludesDefault && everyChainInDefault);
        }

        /// <summary>
        /// Whether any of these groups is one this selection names; false for null, which names nothing. Two
        /// array walks with <c>==</c>, the ordinal comparison that answers on the reference alone for the
        /// interned literals these usually are: it is asked for every grouped chain of every run.
        /// </summary>
        internal bool Selects(string[]? chainGroups)
        {
            if (chainGroups is null)
            {
                return false;
            }

            if (this.IncludesAll)
            {
                return true;
            }

            var names = this.names;

            for (var i = 0; i < names.Length; i++)
            {
                var name = names[i];

                for (var j = 0; j < chainGroups.Length; j++)
                {
                    if (chainGroups[j] == name)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
