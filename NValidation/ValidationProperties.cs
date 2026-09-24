using System.Collections;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using NValidation.Internals;

namespace NValidation
{
    /// <summary>
    /// The properties a validation is limited to, named as failures are reported —
    /// <c>ValidationProperties = ["PurchasePrice", "Model.Name"]</c> — for a check of what one request
    /// changed rather than of the whole object.
    /// </summary>
    /// <remarks>
    /// A chain runs where its property name is one of these or lies below one; a chain whose property lies
    /// on the way to one runs only the part that hands the value to another validator, which is limited to
    /// what lies below. A name without a position applies to every entry of a collection:
    /// <c>ServiceHistory.Workshop</c>. Names are compared ignoring case, because they often come from a
    /// client, and one naming nothing is ignored rather than refused.
    /// </remarks>
    [CollectionBuilder(typeof(ValidationProperties), nameof(Create))]
    public sealed class ValidationProperties : IEnumerable<string>
    {
        private readonly string[] propertyNames;

        private readonly string[] segments;

        private readonly ValidationProperties[] children;

        private readonly bool isSelected;

        private CallInputs? inputs;

        private ValidationProperties(string[] propertyNames, string[] segments, ValidationProperties[] children, bool isSelected)
        {
            this.propertyNames = propertyNames;
            this.segments = segments;
            this.children = children;
            this.isSelected = isSelected;
        }

        /// <summary>
        /// Limits a validation to the named properties.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// No name is given, or a name is blank, has an empty segment, or names a position with <c>[</c>.
        /// </exception>
        public ValidationProperties(params ReadOnlySpan<string> propertyNames)
            : this(Build(propertyNames))
        {
        }

        private ValidationProperties(ValidationProperties built)
            : this(built.propertyNames, built.segments, built.children, built.isSelected)
        {
        }

        /// <summary>
        /// The property names this limits a validation to, relative to where it applies.
        /// </summary>
        public IReadOnlyList<string> PropertyNames => this.propertyNames;

        /// <summary>
        /// Limits a validation to the named properties, for a collection expression:
        /// <c>ValidationProperties properties = ["PurchasePrice", "Model.Name"];</c>
        /// </summary>
        /// <inheritdoc cref="ValidationProperties(ReadOnlySpan{string})" path="/exception"/>
        public static ValidationProperties Create(ReadOnlySpan<string> propertyNames)
        {
            return new ValidationProperties(propertyNames);
        }

        /// <summary>
        /// Limits a validation to the properties <paramref name="properties"/> select, so a rename is
        /// followed by the compiler: <c>ValidationProperties.For&lt;Car&gt;(c => c.PurchasePrice, c => c.Model!.Name)</c>.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// No property is selected, or an expression does not reach a property through its own parameter.
        /// </exception>
        public static ValidationProperties For<T>(params ReadOnlySpan<Expression<Func<T, object?>>> properties)
        {
            var propertyNames = new string[properties.Length];

            for (var i = 0; i < properties.Length; i++)
            {
                ArgumentNullException.ThrowIfNull(properties[i], nameof(properties));

                propertyNames[i] = PropertyPath.From(properties[i]);
            }

            return new ValidationProperties(propertyNames);
        }

        /// <inheritdoc/>
        public IEnumerator<string> GetEnumerator()
        {
            return ((IEnumerable<string>)this.propertyNames).GetEnumerator();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Join(", ", this.propertyNames);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// How a chain reported under <paramref name="chainName"/> is treated: run where it is at or below a
        /// selected name, run through to what it composes where a selected name lies below it, and skipped
        /// otherwise.
        /// </summary>
        internal ChainGate Match(string chainName)
        {
            var node = this.Walk(chainName, out var reachedTheEnd);

            if (node is null)
            {
                return ChainGate.Skip;
            }

            return node.isSelected || !reachedTheEnd ? ChainGate.Run : ChainGate.Composed;
        }

        /// <summary>
        /// What of this selection lies below a chain reported under <paramref name="chainName"/>, for the
        /// validators the chain hands its value to; null where the chain is at or below a selected name, so
        /// everything below it runs.
        /// </summary>
        internal ValidationProperties? Below(string chainName)
        {
            var node = this.Walk(chainName, out var reachedTheEnd);

            return node is null || node.isSelected || !reachedTheEnd ? null : node;
        }

        /// <summary>
        /// This selection paired with the groups and data of a call, kept for the next pass that pairs them
        /// again: one entry, so a selection reused across calls, or handed down to the entries of a
        /// collection, allocates nothing per pass.
        /// </summary>
        internal CallInputs InputsFor(ValidationGroups groups, ValidationData? data)
        {
            var cached = this.inputs;

            if (cached != null && ReferenceEquals(cached.Groups, groups) && ReferenceEquals(cached.Data, data))
            {
                return cached;
            }

            cached = new CallInputs(groups, data, this);
            this.inputs = cached;

            return cached;
        }

        /// <summary>
        /// Follows a dotted name down the tree one segment at a time, without splitting it. Stops early at a
        /// selected node — everything below it is selected — and returns null where the name leaves the tree.
        /// </summary>
        private ValidationProperties? Walk(string name, out bool reachedTheEnd)
        {
            var node = this;
            var rest = name.AsSpan();

            while (rest.Length > 0)
            {
                if (node.isSelected)
                {
                    reachedTheEnd = false;

                    return node;
                }

                var dot = rest.IndexOf('.');
                var segment = dot < 0 ? rest : rest[..dot];

                rest = dot < 0 ? default : rest[(dot + 1)..];
                node = node.Child(segment);

                if (node is null)
                {
                    reachedTheEnd = false;

                    return null;
                }
            }

            reachedTheEnd = true;

            return node;
        }

        private ValidationProperties? Child(ReadOnlySpan<char> segment)
        {
            var segments = this.segments;

            for (var i = 0; i < segments.Length; i++)
            {
                if (segment.Equals(segments[i], StringComparison.OrdinalIgnoreCase))
                {
                    return this.children[i];
                }
            }

            return null;
        }

        private static ValidationProperties Build(ReadOnlySpan<string> propertyNames)
        {
            if (propertyNames.Length == 0)
            {
                throw new ArgumentException("At least one property has to be named.", nameof(propertyNames));
            }

            var root = new Node();

            foreach (var propertyName in propertyNames)
            {
                root.Add(Split(propertyName));
            }

            return root.Freeze();
        }

        private static string[] Split(string? propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentException("A property name has to say something, so it cannot be empty.", "propertyNames");
            }

            if (propertyName.Contains('[', StringComparison.Ordinal) || propertyName.Contains(']', StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"'{propertyName}' names a position. Name the property without one — ServiceHistory.Workshop — " +
                    "and it applies to every entry.",
                    "propertyNames");
            }

            var segments = propertyName.Split('.');

            foreach (var segment in segments)
            {
                if (string.IsNullOrWhiteSpace(segment))
                {
                    throw new ArgumentException($"'{propertyName}' has an empty segment.", "propertyNames");
                }
            }

            return segments;
        }

        /// <summary>
        /// The tree while it is built, before it is frozen into selections that cannot change.
        /// </summary>
        private sealed class Node
        {
            private readonly List<(string Segment, Node Node)> children = [];

            private bool isSelected;

            public void Add(string[] segments)
            {
                var node = this;

                foreach (var segment in segments)
                {
                    var next = node.Find(segment);

                    if (next is null)
                    {
                        next = new Node();
                        node.children.Add((segment, next));
                    }

                    node = next;
                }

                node.isSelected = true;
            }

            public ValidationProperties Freeze()
            {
                var propertyNames = new List<string>();
                this.CollectNames(string.Empty, propertyNames);

                // A selected name selects everything below it, so what was named below it adds nothing.
                if (this.isSelected)
                {
                    return new ValidationProperties([.. propertyNames], [], [], isSelected: true);
                }

                var segments = new string[this.children.Count];
                var children = new ValidationProperties[this.children.Count];

                for (var i = 0; i < this.children.Count; i++)
                {
                    segments[i] = this.children[i].Segment;
                    children[i] = this.children[i].Node.Freeze();
                }

                return new ValidationProperties([.. propertyNames], segments, children, isSelected: false);
            }

            private Node? Find(string segment)
            {
                foreach (var (candidate, node) in this.children)
                {
                    if (string.Equals(candidate, segment, StringComparison.OrdinalIgnoreCase))
                    {
                        return node;
                    }
                }

                return null;
            }

            private void CollectNames(string prefix, List<string> propertyNames)
            {
                if (this.isSelected)
                {
                    propertyNames.Add(prefix.Length == 0 ? string.Empty : prefix[..^1]);

                    return;
                }

                foreach (var (segment, node) in this.children)
                {
                    node.CollectNames($"{prefix}{segment}.", propertyNames);
                }
            }
        }
    }
}
