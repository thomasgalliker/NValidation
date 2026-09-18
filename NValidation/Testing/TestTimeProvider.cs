namespace NValidation.Testing
{
    /// <summary>
    /// A clock a test controls, so a rule which compares against "now" has a fixed answer instead of one
    /// that depends on when the suite runs. Hand it to the rule:
    /// <code>
    /// var clock = new TestTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    ///
    /// var validator = new TestValidator&lt;Invoice&gt;();
    /// validator.Property(i => i.DueDate).InTheFuture(clock);
    /// </code>
    /// </summary>
    public sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset utcNow;

        /// <summary>
        /// A clock reading <paramref name="utcNow"/> until something moves it.
        /// </summary>
        public TestTimeProvider(DateTimeOffset utcNow)
        {
            this.utcNow = utcNow;
        }

        /// <inheritdoc/>
        public override DateTimeOffset GetUtcNow()
        {
            return this.utcNow;
        }

        /// <summary>
        /// Moves the clock on by <paramref name="duration"/>, for a test about what happens once time has
        /// passed. A negative duration moves it back.
        /// </summary>
        public void Advance(TimeSpan duration)
        {
            this.utcNow = this.utcNow.Add(duration);
        }
    }
}
