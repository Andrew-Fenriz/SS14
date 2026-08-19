using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Verbs.Operations.Smites;

/// <summary>
/// Spawns a storage entity, stuffs the target into it, and welds it shut.
/// </summary>
public sealed partial class StuffIntoLockerOperation : AdminOperationBase<StuffIntoLockerOperation>
{
    [DataField(required: true)]
    public EntProtoId Prototype { get; private set; }
}
