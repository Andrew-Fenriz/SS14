using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Verbs.Operations.Smites;

/// <summary>
/// Spawns the configured homing entity at a random offset from the target and configures it to chase them.
/// </summary>
public sealed partial class HomingRodOperation : AdminOperationBase<HomingRodOperation>
{
    /// <summary>
    /// Entity prototype to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype { get; private set; }

    /// <summary>
    /// Distance from the target at which the entity is spawned.
    /// </summary>
    [DataField(required: true)]
    public float Distance { get; private set; }

    /// <summary>
    /// Base chase speed.
    /// </summary>
    [DataField(required: true)]
    public float Speed { get; private set; }

    /// <summary>
    /// Whether the target's current sprint speed should override <see cref="Speed"/> when available.
    /// </summary>
    [DataField]
    public bool MatchTargetSprintSpeed { get; private set; }
}
