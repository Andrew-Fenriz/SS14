using Content.Shared.Interaction.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Verbs.Operations;

/// <summary>
/// Spawns and force-equips configured entity prototypes into inventory slots,
/// optionally clearing other slots and making equipped clothing unremoveable.
/// </summary>
public sealed partial class SetEquipmentOperation : AdminOperationBase<SetEquipmentOperation>
{
    /// <summary>
    /// Inventory slot IDs mapped to entity prototypes to spawn and equip.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, EntProtoId> Equipment { get; private set; } = new();

    /// <summary>
    /// Whether every inventory slot is force-unequipped before applying <see cref="Equipment"/>.
    /// When false, only configured destination slots are cleared.
    /// </summary>
    [DataField]
    public bool ClearOtherSlots { get; private set; }

    /// <summary>
    /// Whether successfully equipped clothing receives <see cref="UnremoveableComponent"/>.
    /// </summary>
    [DataField]
    public bool Unremoveable { get; private set; }
}
