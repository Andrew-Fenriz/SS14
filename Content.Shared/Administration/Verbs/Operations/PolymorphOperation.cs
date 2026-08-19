using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Verbs.Operations;

// TODO: Use EntityEffectsOperation once the Polymorph entity effect no longer requires PolymorphableComponent.
/// <summary>
/// Polymorphs the target using a polymorph prototype.
/// </summary>
public sealed partial class PolymorphOperation : AdminOperationBase<PolymorphOperation>
{
    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> Prototype { get; private set; }
}
