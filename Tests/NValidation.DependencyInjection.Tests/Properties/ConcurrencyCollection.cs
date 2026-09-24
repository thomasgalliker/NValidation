using Xunit;

/// <summary>
/// Tests which saturate the thread pool cannot run next to anything else: xUnit runs collections in
/// parallel up to the core count by default, so a test that is trying to maximise interleaving would
/// be competing with the rest of the suite for the very threads it needs.
/// </summary>
[CollectionDefinition(Collections.Concurrency, DisableParallelization = true)]
public class ConcurrencyCollection;
