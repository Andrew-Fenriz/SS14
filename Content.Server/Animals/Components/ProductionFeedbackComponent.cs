using Content.Server.Animals.Systems;
using Robust.Shared.Audio;

namespace Content.Server.Animals.Components;

/// <summary>
/// Defines optional feedback for production attempts.
/// </summary>
[RegisterComponent, Access(typeof(ProductionFeedbackSystem))]
public sealed partial class ProductionFeedbackComponent : Component
{
    /// <summary>
    /// Sound played after successful production.
    /// </summary>
    [DataField]
    public SoundSpecifier? ProductionSound;

    /// <summary>
    /// Popup shown to the requester when production fails due to insufficient satiation.
    /// </summary>
    [DataField]
    public LocId? InsufficientSatiationPopup;

    /// <summary>
    /// Popup shown to the producer after successful production.
    /// </summary>
    [DataField]
    public LocId? UserPopup;

    /// <summary>
    /// Popup shown to nearby observers after successful production.
    /// </summary>
    [DataField]
    public LocId? OthersPopup;
}
