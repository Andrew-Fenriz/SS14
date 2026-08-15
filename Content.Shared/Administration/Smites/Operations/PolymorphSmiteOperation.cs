using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Smites.Operations;

/// <summary>
/// Polymorphs the smite target using an existing polymorph prototype.
/// </summary>
public sealed partial class PolymorphSmite : SmiteOperationBase<PolymorphSmite>
{
    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> Prototype { get; private set; }
}
