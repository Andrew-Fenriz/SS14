using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Smites.Operations;

/// <summary>
/// Adds configured components to the smite target, optionally replacing existing components.
/// </summary>
public sealed partial class AddComponentsSmite : SmiteOperationBase<AddComponentsSmite>
{
    [DataField(required: true)]
    public ComponentRegistry Components { get; private set; } = new();

    /// <summary>
    /// Whether configured components should replace components already present on the target.
    /// </summary>
    [DataField]
    public bool ReplaceExisting { get; private set; }
}
