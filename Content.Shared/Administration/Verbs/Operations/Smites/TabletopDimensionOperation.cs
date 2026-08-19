using Content.Shared.Tabletop.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Verbs.Operations.Smites;

/// <summary>
/// Sends the target into a tabletop game session created from the configured prototype.
/// </summary>
public sealed partial class TabletopDimensionOperation : AdminOperationBase<TabletopDimensionOperation>
{
    /// <summary>
    /// Tabletop game prototype used to create the session.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<TabletopGameComponent> Prototype { get; private set; }
}
