/// <summary>
/// Names of the xUnit collections which cannot run beside anything else.
/// </summary>
public static class Collections
{
    /// <summary>
    /// Tests which boot the sample application. They share one host, so they run in sequence.
    /// </summary>
    public const string SampleApi = "SampleApi";
}
