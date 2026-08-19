namespace Content.Shared.Administration.Verbs.Operations.Smites;

/// <summary>
/// Disconnects the target with a localized reason.
/// </summary>
public sealed partial class GhostKickOperation : AdminOperationBase<GhostKickOperation>
{
    [DataField(required: true)]
    public LocId Reason { get; private set; }
}
