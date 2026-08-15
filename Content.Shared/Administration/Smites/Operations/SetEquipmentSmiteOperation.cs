using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Smites.Operations;

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
