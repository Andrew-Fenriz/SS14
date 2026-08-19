namespace Content.Shared.Administration.Verbs.Operations;

/// <summary>
/// Sets whether godmode is enabled on the target.
/// </summary>
public sealed partial class SetGodmodeOperation : AdminOperationBase<SetGodmodeOperation>
{
    [DataField(required: true)]
    public bool Enabled { get; private set; }
}
