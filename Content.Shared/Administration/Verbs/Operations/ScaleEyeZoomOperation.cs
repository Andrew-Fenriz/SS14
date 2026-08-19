namespace Content.Shared.Administration.Verbs.Operations;

/// <summary>
/// Scales the target's current eye zoom by the configured factor.
/// </summary>
public sealed partial class ScaleEyeZoomOperation : AdminOperationBase<ScaleEyeZoomOperation>
{
    /// <summary>
    /// Multiplier applied to both axes of the current target zoom.
    /// </summary>
    [DataField(required: true)]
    public float Factor { get; private set; }
}
