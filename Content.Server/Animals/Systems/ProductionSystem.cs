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
    /// <param name="requester">Entity that explicitly requested the production, if any.</param>
    public bool TryProduce(
        EntityUid source,
        EntityUid producer,
        EntityUid? requester = null)
    {
        var before = new BeforeProductionEvent(producer, requester);
        RaiseLocalEvent(source, ref before);
        if (before.Cancelled)
            return false;

        var attempt = new ProductionAttemptEvent(producer, requester);
        RaiseLocalEvent(source, ref attempt);
        if (!attempt.Produced)
            return false;

        var completed = new ProductionCompletedEvent(producer, requester);
        RaiseLocalEvent(source, ref completed);

        return true;
    }
}

/// <summary>
/// Raised before production is attempted, allowing production requirements to cancel it.
/// </summary>
/// <param name="Producer">Entity performing the production.</param>
/// <param name="Requester">Entity that explicitly requested the production, if any.</param>
[ByRefEvent]
public record struct BeforeProductionEvent(
    EntityUid Producer,
    EntityUid? Requester)
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
/// <param name="Requester">Entity that explicitly requested the production, if any.</param>
[ByRefEvent]
public record struct ProductionAttemptEvent(
    EntityUid Producer,
    EntityUid? Requester)
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
/// <param name="Requester">Entity that explicitly requested the production, if any.</param>
[ByRefEvent]
public readonly record struct ProductionCompletedEvent(
    EntityUid Producer,
    EntityUid? Requester);
