using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Verbs.Operations;

/// <summary>
/// Adds an action to the target if it does not already have one from the same prototype.
/// </summary>
public sealed partial class AddActionOperation : AdminOperationBase<AddActionOperation>
{
    [DataField(required: true)]
    public EntProtoId Action { get; private set; }
}
