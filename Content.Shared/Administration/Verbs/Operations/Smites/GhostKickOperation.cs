namespace Content.Shared.Administration.Verbs.Operations.Smites;

/// <summary>
/// Disconnects the target from their current session.
/// </summary>
public sealed partial class GhostKickOperation : AdminOperationBase<GhostKickOperation>
{
    /// <summary>
    /// The reason that will be displayed in the server log when the target is disconnected.
    /// </summary>
    [DataField(required: true)]
    public LocId Reason { get; private set; }
}
