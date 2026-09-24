namespace NValidation.Internals
{
    /// <summary>
    /// Where one chain runs, settled when its validator freezes: the groups it is in, whether it is in the
    /// default group too, and the groups a validator it hands its value to declares. <c>default</c> is a
    /// chain in the default group alone, so an array of these starts out describing plain chains.
    /// </summary>
    internal readonly struct ChainGroups
    {
        private readonly bool isOutsideDefault;

        private ChainGroups(string[]? named, bool isOutsideDefault, string[]? reach)
        {
            this.Named = named;
            this.isOutsideDefault = isOutsideDefault;
            this.Reach = reach;
        }

        /// <summary>
        /// The groups the chain was put in, without the default group; null where it is in none.
        /// </summary>
        public string[]? Named { get; }

        public bool InDefault => !this.isOutsideDefault;

        /// <summary>
        /// The groups the validators this chain composes declare, through which a selection that leaves
        /// the default group out still reaches them. Only a chain in the default group has any: a chain
        /// put in groups of its own is reached by those names alone.
        /// </summary>
        public string[]? Reach { get; }

        public bool IsPlain => this.Named is null && this.Reach is null && !this.isOutsideDefault;

        public static ChainGroups Of(string[]? groups, string[]? composedGroups)
        {
            if (groups is null)
            {
                return new ChainGroups(named: null, isOutsideDefault: false, composedGroups);
            }

            var inDefault = GroupNames.Contains(groups, ValidationGroups.DefaultGroup);
            var named = GroupNames.WithoutDefault(groups);

            return new ChainGroups(
                named.Length == 0 ? null : named,
                isOutsideDefault: !inDefault,
                inDefault ? composedGroups : null);
        }
    }

    /// <summary>
    /// Where each chain of a validator runs, in the order the rules are walked, laid out as parallel arrays
    /// so that a chain its own groups select costs the gate one reference, as it would with nothing else to
    /// know.
    /// </summary>
    internal sealed class ChainGates
    {
        private readonly bool[]? alsoInDefault;

        private readonly string[]?[]? reach;

        private ChainGates(string[]?[] named, bool[]? alsoInDefault, string[]?[]? reach)
        {
            this.Named = named;
            this.alsoInDefault = alsoInDefault;
            this.reach = reach;
        }

        /// <summary>
        /// The named groups of each chain; null for a chain in the default group alone.
        /// </summary>
        public string[]?[] Named { get; }

        public static ChainGates From(ChainGroups[] chains)
        {
            var named = new string[]?[chains.Length];
            bool[]? alsoInDefault = null;
            string[]?[]? reach = null;

            for (var i = 0; i < chains.Length; i++)
            {
                var chain = chains[i];

                named[i] = chain.Named;

                if (chain.Named != null && chain.InDefault)
                {
                    alsoInDefault ??= new bool[chains.Length];
                    alsoInDefault[i] = true;
                }

                if (chain.Reach != null)
                {
                    reach ??= new string[]?[chains.Length];
                    reach[i] = chain.Reach;
                }
            }

            return new ChainGates(named, alsoInDefault, reach);
        }

        public ChainGate Gate(int index, ValidationGroups selection)
        {
            var named = this.Named[index];

            return (named is null ? selection.IncludesDefault : selection.Selects(named))
                ? ChainGate.Run
                : this.GateOfUnselected(index, selection);
        }

        /// <summary>
        /// The gate of a chain its own groups leave out of <paramref name="selection"/>: it runs where it is in
        /// the default group as well and the selection includes that, runs its composed part where the
        /// selection reaches through it, and is skipped otherwise.
        /// </summary>
        public ChainGate GateOfUnselected(int index, ValidationGroups selection)
        {
            if (selection.IncludesDefault)
            {
                return this.alsoInDefault is { } also && also[index] ? ChainGate.Run : ChainGate.Skip;
            }

            return this.reach is { } reach && selection.Selects(reach[index]) ? ChainGate.Composed : ChainGate.Skip;
        }
    }
}
