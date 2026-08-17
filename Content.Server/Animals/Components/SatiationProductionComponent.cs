using Content.Server.Animals.Systems;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Animals.Components;

/// <summary>
/// Periodically attempts to produce something, consuming satiation on success.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(SatiationProductionSystem))]
public sealed partial class SatiationProductionComponent : Component
{
    /// <summary>
    /// Selects the entity whose mob state and satiation are used for production checks and consumption.
    /// </summary>
    [DataField]
    public SatiationProductionOwner Producer = SatiationProductionOwner.Self;

    /// <summary>
    /// Minimum delay between automatic production attempts.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Optional maximum delay. When set, each automatic delay is randomized.
    /// </summary>
    [DataField]
    public TimeSpan? DelayMax;

    /// <summary>
    /// Amount of the configured satiation removed after successful production.
    /// </summary>
    [DataField]
    public float SatiationUsage = 10f;

    /// <summary>
    /// Satiation type checked and consumed by production. Defaults to hunger.
    /// </summary>
    [DataField]
    public ProtoId<SatiationTypePrototype> SatiationType = SatiationSystem.Hunger;

    /// <summary>
    /// Optional threshold that the configured satiation must still exceed after applying the production cost.
    /// </summary>
    [DataField]
    public SatiationValue? MinimumSatiationThreshold;

    /// <summary>
    /// Optional minimum numeric value of the configured satiation required before production.
    /// </summary>
    [DataField]
    public float? MinimumSatiation;

    /// <summary>
    /// Whether production is attempted automatically.
    /// </summary>
    [DataField]
    public bool Automatic = true;

    /// <summary>
    /// Whether automatic production is allowed for player-controlled producer entities.
    /// </summary>
    [DataField]
    public bool AutomaticForPlayers = true;

    /// <summary>
    /// Next scheduled automatic production attempt. Adjusted while the component is paused.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextProductionTime;
}

/// <summary>
/// Selects the entity against which production conditions are evaluated.
/// </summary>
public enum SatiationProductionOwner : byte
{
    Self,
    Parent
}

/// <summary>
/// Reason a satiation production requirement failed.
/// </summary>
public enum SatiationProductionFailure : byte
{
    None,
    Dead,

    /// <summary>
    /// The selected producer does not meet the configured satiation requirement.
    /// </summary>
    InsufficientSatiation
}

/// <summary>
/// Raised when production is cancelled by a satiation production requirement.
/// </summary>
/// <param name="Producer">Entity whose satiation was checked.</param>
/// <param name="Requester">Entity that explicitly requested the production, if any.</param>
/// <param name="Failure">Reason the satiation production requirement failed.</param>
[ByRefEvent]
public readonly record struct SatiationProductionFailedEvent(
    EntityUid Producer,
    EntityUid? Requester,
    SatiationProductionFailure Failure);
