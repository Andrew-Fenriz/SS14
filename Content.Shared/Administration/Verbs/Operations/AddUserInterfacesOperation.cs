namespace Content.Shared.Administration.Verbs.Operations;

/// <summary>
/// Adds or replaces bound user interfaces on the target.
/// </summary>
public sealed partial class AddUserInterfacesOperation : AdminOperationBase<AddUserInterfacesOperation>
{
    [DataField(required: true)]
    public Dictionary<Enum, InterfaceData> Interfaces { get; private set; } = new();
}
