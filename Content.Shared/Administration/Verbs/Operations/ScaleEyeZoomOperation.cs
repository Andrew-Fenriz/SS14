namespace Content.Shared.Administration.Verbs.Operations;

/// <summary>
/// Scales the target's current eye zoom.
/// </summary>
public sealed partial class ScaleEyeZoomOperation : AdminOperationBase<ScaleEyeZoomOperation>
{
    [DataField(required: true)]
    public float Factor { get; private set; }
}
