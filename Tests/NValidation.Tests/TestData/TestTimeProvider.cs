namespace NValidation.Tests.TestData
{
    /// <summary>
    /// A clock a test controls, so a rule which compares against "now" has a fixed answer instead of
    /// one that depends on when the suite runs.
    /// </summary>
    internal sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset utcNow;

        public TestTimeProvider(DateTimeOffset utcNow)
        {
            this.utcNow = utcNow;
        }

        /// <summary>
        /// A fixed instant the fixtures are built around, so "in the past" and "in the future" are
        /// unambiguous without any arithmetic in the tests.
        /// </summary>
        public static TestTimeProvider AtStartOf2026()
        {
            return new TestTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        }

        public override DateTimeOffset GetUtcNow()
        {
            return this.utcNow;
        }

        public void Advance(TimeSpan duration)
        {
            this.utcNow = this.utcNow.Add(duration);
        }
    }
}
