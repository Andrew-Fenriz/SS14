using Content.Shared.EntityEffects;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Smites;

/// <summary>
/// Adds configured components to the smite target, optionally replacing existing components.
/// </summary>
public sealed partial class AddComponentsSmite : SmiteOperationBase<AddComponentsSmite>
{
    [DataField(required: true)]
    public ComponentRegistry Components { get; private set; } = new();

    /// <summary>
    /// Whether configured components should replace components already present on the target.
    /// </summary>
    [DataField]
    public bool ReplaceExisting { get; private set; }
}

/// <summary>
/// Applies a configured set of entity effects to the smite target.
/// </summary>
public sealed partial class EntityEffectsSmite : SmiteOperationBase<EntityEffectsSmite>
{
    [DataField(required: true)]
    public EntityEffect[] Effects { get; private set; } = [];
}

/// <summary>
/// Launches an immovable rod that chases the smite target.
/// </summary>
public sealed partial class HomingRodSmite : SmiteOperationBase<HomingRodSmite>
{
    [DataField(required: true)]
    public EntProtoId Prototype { get; private set; }

    [DataField(required: true)]
    public float Distance { get; private set; }

    [DataField(required: true)]
    public float Speed { get; private set; }

    [DataField]
    public bool MatchTargetSprintSpeed { get; private set; }
}

/// <summary>
/// Polymorphs the smite target using an existing polymorph prototype.
/// </summary>
public sealed partial class PolymorphSmite : SmiteOperationBase<PolymorphSmite>
{
    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> Prototype { get; private set; }
}

/// <summary>
/// Spawns a storage entity, stuffs the smite target into it, and welds it shut.
/// </summary>
public sealed partial class StuffIntoLockerSmite : SmiteOperationBase<StuffIntoLockerSmite>
{
    [DataField(required: true)]
    public EntProtoId Prototype { get; private set; }
}
