using System.Collections;
using System.Runtime.CompilerServices;
using NValidation.Internals;

namespace NValidation
{
    /// <summary>
    /// Which rule groups a validation runs, on top of the chains in no group, which run whatever this
    /// says: <c>ValidationGroups.None</c>, <c>ValidationGroups.All</c>, or the names to select.
    /// </summary>
    /// <remarks>
    /// Immutable, and converted from what a caller would otherwise write by hand: a name, an array of
    /// names, or a collection expression — <c>ValidationGroups = "Create"</c> and
    /// <c>ValidationGroups = ["Create", "Update"]</c> both compile. Names are compared ordinally.
    /// Carried by <see cref="NValidationOptions.ValidationGroups"/> and resolved per run, so a validator
    /// never selects groups for itself.
    /// </remarks>
    [CollectionBuilder(typeof(ValidationGroups), nameof(Create))]
    public sealed class ValidationGroups : IEnumerable<string>
    {
        private readonly string[] names;

        private ValidationGroups(string[] names, bool includesAll)
        {
            this.names = names;
            this.IncludesAll = includesAll;
        }

        /// <summary>
        /// Selects the chains in the named groups; naming none is <see cref="None"/>.
        /// </summary>
        /// <exception cref="ArgumentException">A name is <c>null</c>, empty or whitespace.</exception>
        public ValidationGroups(params ReadOnlySpan<string> groups)
            : this(GroupNames.Copy(groups, nameof(groups)), includesAll: false)
        {
        }

        /// <summary>
        /// Only the chains in no group run. What a validation selects unless something asked for more.
        /// </summary>
        public static ValidationGroups None { get; } = new(GroupNames.Empty, includesAll: false);

        /// <summary>
        /// Every chain runs, whatever group it is in.
        /// </summary>
        public static ValidationGroups All { get; } = new(GroupNames.Empty, includesAll: true);

        /// <summary>
        /// The groups this selects by name; empty for <see cref="None"/> and for <see cref="All"/>, which
        /// select by what they are rather than by naming anything.
        /// </summary>
        public IReadOnlyList<string> Names => this.names;

        /// <summary>
        /// Whether every grouped chain is selected, whatever its group is called.
        /// </summary>
        public bool IncludesAll { get; }

        /// <summary>
        /// Selects the chains in the named groups, for a collection expression:
        /// <c>ValidationGroups groups = ["Create", "Update"];</c>
        /// </summary>
        /// <inheritdoc cref="ValidationGroups(ReadOnlySpan{string})" path="/exception"/>
        public static ValidationGroups Create(ReadOnlySpan<string> groups)
        {
            return groups.Length == 0 ? None : new ValidationGroups(groups);
        }

        /// <summary>
        /// Selects one group, so a caller writing a single name does not have to say the type:
        /// <c>ValidationGroups = "Create"</c>.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="group"/> is empty or whitespace.</exception>
        public static implicit operator ValidationGroups(string? group)
        {
            return group is null ? None : new ValidationGroups(group);
        }

        /// <summary>
        /// Selects the groups an array names, for a caller which already holds one.
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

            return this.names.Length == 0 ? "None" : string.Join(", ", this.names);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        // Whether a chain in these groups is one this selection runs. Two array walks rather than LINQ or
        // a set, and the comparison written out rather than delegated: this is asked for every grouped
        // chain of every run, and both arrays are all but always one name long. The == operator is the
        // ordinal comparison, and it answers on the reference alone for the interned literals these
        // usually are.
        internal bool Selects(string[] chainGroups)
        {
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
