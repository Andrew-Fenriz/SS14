namespace Content.Server.Animals.Systems;

/// <summary>
/// Handles dispatching production attempts.
/// </summary>
public sealed partial class ProductionSystem : EntitySystem
{
    /// <summary>
    /// Attempts to produce something using the production handlers on <paramref name="source"/>.
    /// </summary>
    /// <param name="source">Entity containing the production configuration.</param>
    /// <param name="producer">Entity performing the production.</param>
    public bool TryProduce(EntityUid source, EntityUid producer)
    {
        var ev = new ProductionAttemptEvent(producer);
        RaiseLocalEvent(source, ref ev);
        return ev.Produced;
    }
}

/// <summary>
/// Raised when production is attempted.
/// Handlers set <see cref="Produced"/> when something was successfully produced.
/// </summary>
/// <param name="Producer">Entity performing the production.</param>
[ByRefEvent]
public record struct ProductionAttemptEvent(EntityUid Producer)
{
    /// <summary>
    /// Set by handlers when production succeeds.
    /// </summary>
    public bool Produced;
}
