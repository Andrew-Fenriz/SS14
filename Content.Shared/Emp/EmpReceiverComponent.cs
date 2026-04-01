using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Emp;

/// <summary>
///     This component allows an entity to react to EMP pulses by taking damage
///     and potentially being disabled.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed class EmpReceiverComponent : Component
{
    /// <summary>
    ///     The base amount of damage dealt when an EMP pulse hits the entity.
    /// </summary>
    [DataField] [AutoNetworkedField] public float Damage = 10f;

    /// <summary>
    ///     The type of damage applied to the entity.
    /// </summary>
    [DataField] [AutoNetworkedField] public ProtoId<DamageTypePrototype> DamageType = "Shock";

    /// <summary>
    ///     Whether to scale the damage based on the energy consumption of the EMP pulse.
    ///     If true, damage will be multiplied by the pulse's EnergyConsumption value.
    /// </summary>
    [DataField] [AutoNetworkedField] public bool ScaleByEnergy;
}
