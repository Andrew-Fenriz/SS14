namespace Content.Shared.Administration.Verbs.Operations;

/// <summary>
/// Sets the target's godmode state.
/// </summary>
public sealed partial class SetGodmodeOperation : AdminOperationBase<SetGodmodeOperation>
{
    [DataField(required: true)]
    public bool Enabled { get; private set; }
}
