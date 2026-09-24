using Xunit;

/// <summary>
/// Tests which change the ambient culture cannot run next to anything else, because the culture is
/// process-wide.
/// </summary>
[CollectionDefinition(Collections.CultureSpecific, DisableParallelization = true)]
public class CultureSpecificCollection;
