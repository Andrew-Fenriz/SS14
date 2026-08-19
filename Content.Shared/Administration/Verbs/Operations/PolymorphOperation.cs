using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Verbs.Operations;

/// <summary>
/// Polymorphs the target using an existing polymorph prototype.
/// </summary>
public sealed partial class PolymorphOperation : AdminOperationBase<PolymorphOperation>
{
    /// <summary>
    /// Polymorph prototype used for the transformation.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> Prototype { get; private set; }
}
