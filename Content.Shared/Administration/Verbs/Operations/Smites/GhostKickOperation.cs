namespace Content.Shared.Administration.Verbs.Operations.Smites;

/// <summary>
/// Disconnects the target from their current session.
/// </summary>
public sealed partial class GhostKickOperation : AdminOperationBase<GhostKickOperation>
{
    /// <summary>
    /// Localization key used as the disconnect reason.
    /// </summary>
    [DataField(required: true)]
    public LocId Reason { get; private set; }
}
