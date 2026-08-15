using Content.Shared.EntityEffects;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Smites.Operations;

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
/// Displays one localized popup for a smite target.
/// </summary>
public sealed partial class PopupSmite : SmiteOperationBase<PopupSmite>
{
    [DataField(required: true)]
    public LocId Message { get; private set; }

    [DataField]
    public SmitePopupRecipients Recipients { get; private set; } = SmitePopupRecipients.Target;

    [DataField]
    public SmitePopupLocation Location { get; private set; } = SmitePopupLocation.Entity;

    [DataField]
    public PopupType Type { get; private set; } = PopupType.Small;
}

public enum SmitePopupRecipients : byte
{
    Target,
    Pvs,
    PvsExceptTarget
}

public enum SmitePopupLocation : byte
{
    Entity,
    Coordinates
}

/// <summary>
/// Force-equips configured entity prototypes into inventory slots.
/// </summary>
public sealed partial class SetEquipmentSmite : SmiteOperationBase<SetEquipmentSmite>
{
    [DataField(required: true)]
    public Dictionary<string, EntProtoId> Equipment { get; private set; } = new();

    [DataField]
    public bool ClearOtherSlots { get; private set; }

    [DataField]
    public bool Unremoveable { get; private set; }
}

/// <summary>
/// Swaps the smite target's base walking and sprinting speeds.
/// </summary>
public sealed partial class SwapMovementSpeedsSmite : SmiteOperationBase<SwapMovementSpeedsSmite>;
