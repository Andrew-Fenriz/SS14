namespace Content.Server.Animals.Systems;

/// <summary>
/// Handles dispatching production attempts and their lifecycle.
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
        var before = new BeforeProductionEvent(producer);
        RaiseLocalEvent(source, ref before);
        if (before.Cancelled)
            return false;

        var attempt = new ProductionAttemptEvent(producer);
        RaiseLocalEvent(source, ref attempt);
        if (!attempt.Produced)
            return false;

        var completed = new ProductionCompletedEvent(producer);
        RaiseLocalEvent(source, ref completed);

        return true;
    }
}

/// <summary>
/// Raised before production is attempted, allowing production requirements to cancel it.
/// </summary>
/// <param name="Producer">Entity performing the production.</param>
[ByRefEvent]
public record struct BeforeProductionEvent(EntityUid Producer)
{
    /// <summary>
    /// Whether production should be cancelled.
    /// </summary>
    public bool Cancelled;
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

/// <summary>
/// Raised after something was successfully produced.
/// </summary>
/// <param name="Producer">Entity performing the production.</param>
[ByRefEvent]
public readonly record struct ProductionCompletedEvent(EntityUid Producer);
