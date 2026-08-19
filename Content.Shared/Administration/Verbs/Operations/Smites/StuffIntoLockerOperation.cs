using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Verbs.Operations.Smites;

/// <summary>
/// Spawns the configured storage entity at the target, uses its normal close behavior to capture the target,
/// and welds it shut when supported.
/// </summary>
public sealed partial class StuffIntoLockerOperation : AdminOperationBase<StuffIntoLockerOperation>
{
    /// <summary>
    /// Entity prototype to spawn as the storage.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype { get; private set; }
}
