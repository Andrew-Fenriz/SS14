using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Verbs.Operations.Smites;

/// <summary>
/// Launches an immovable rod that chases the target.
/// </summary>
public sealed partial class HomingRodOperation : AdminOperationBase<HomingRodOperation>
{
    [DataField(required: true)]
    public EntProtoId Prototype { get; private set; }

    [DataField(required: true)]
    public float Distance { get; private set; }

    [DataField(required: true)]
    public float Speed { get; private set; }

    [DataField]
    public bool MatchTargetSprintSpeed { get; private set; }
}
