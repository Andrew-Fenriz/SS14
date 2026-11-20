using Content.Shared.Actions;

namespace Content.Shared.Mech;

/// <summary>
/// Event raised on equipment when it is inserted into a mech
/// </summary>
[ByRefEvent]
public readonly record struct MechEquipmentInsertedEvent(EntityUid Mech)
{
    public readonly EntityUid Mech = Mech;
}

/// <summary>
/// Event raised on equipment when it is removed from a mech
/// </summary>
[ByRefEvent]
public readonly record struct MechEquipmentRemovedEvent(EntityUid Mech)
{
    public readonly EntityUid Mech = Mech;
}

/// <summary>
/// Raised on the mech equipment before it is going to be removed.
/// </summary>
[ByRefEvent]
public record struct AttemptRemoveMechEquipmentEvent()
{
    public bool Cancelled = false;
}

public sealed partial class MechToggleEquipmentEvent : InstantActionEvent;

public sealed partial class MechOpenUiEvent : InstantActionEvent;

public sealed partial class MechEjectPilotEvent : InstantActionEvent;
