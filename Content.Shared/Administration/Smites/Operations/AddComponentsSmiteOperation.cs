using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Smites.Operations;

/// <summary>
/// Adds configured components to the smite target if they are not already present.
/// </summary>
public sealed partial class AddComponentsSmite : SmiteOperationBase<AddComponentsSmite>
{
    [DataField(required: true)]
    public ComponentRegistry Components { get; private set; } = new();
}
