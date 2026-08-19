namespace Content.Shared.Administration.Verbs.Operations;

/// <summary>
/// Adds or replaces configured bound UI entries while preserving unrelated interfaces.
/// </summary>
public sealed partial class AddUserInterfacesOperation : AdminOperationBase<AddUserInterfacesOperation>
{
    /// <summary>
    /// Bound UI definitions keyed by their interface key.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<Enum, InterfaceData> Interfaces { get; private set; } = new();
}
