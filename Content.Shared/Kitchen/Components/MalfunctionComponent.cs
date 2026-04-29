using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Kitchen.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class MalfunctionComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan NextCheckTime = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public float CheckInterval = 1.0f;

    [DataField, AutoNetworkedField]
    public float ExplosionChance = 0.1f;

    [DataField, AutoNetworkedField]
    public float LightningChance = 0.75f;

    [DataField]
    public EntProtoId SparkPrototype = "Spark";
}
