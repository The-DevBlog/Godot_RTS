/// <summary>
/// Implement on anything that has a build/spawn cost.
/// </summary>
public interface ICostProvider
{
    /// <summary>
    /// How many funds (or other resource) it takes to create this instance.
    /// </summary>
    public int Cost { get; }
}