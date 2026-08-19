using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Verbs.Operations;

/// <summary>
/// Adds a mind role to the target's mind if it does not already have one from the same prototype.
/// </summary>
public sealed partial class AddMindRoleOperation : AdminOperationBase<AddMindRoleOperation>
{
    [DataField(required: true)]
    public EntProtoId Role { get; private set; }
}
