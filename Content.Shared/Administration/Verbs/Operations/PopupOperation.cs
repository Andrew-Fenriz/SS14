using Content.Shared.Popups;

namespace Content.Shared.Administration.Verbs.Operations;

/// <summary>
/// Displays a localized popup associated with the target to the configured recipients.
/// </summary>
public sealed partial class PopupOperation : AdminOperationBase<PopupOperation>
{
    /// <summary>
    /// Localization key for the popup.
    /// The target is supplied as both <c>$name</c> and <c>$entity</c>.
    /// </summary>
    [DataField(required: true)]
    public LocId Message { get; private set; }

    /// <summary>
    /// Who receives the popup.
    /// </summary>
    [DataField]
    public PopupRecipients Recipients { get; private set; } = PopupRecipients.Target;

    /// <summary>
    /// Whether the popup is anchored to the target entity or its current coordinates.
    /// </summary>
    [DataField]
    public PopupLocation Location { get; private set; } = PopupLocation.Entity;

    /// <summary>
    /// Presentation style used for the popup.
    /// </summary>
    [DataField]
    public PopupType Type { get; private set; } = PopupType.Small;
}

/// <summary>
/// Recipients supported by <see cref="PopupOperation"/>.
/// </summary>
public enum PopupRecipients : byte
{
    Target,
    Pvs,
    PvsExceptTarget
}

/// <summary>
/// Anchor location used by <see cref="PopupOperation"/>.
/// </summary>
public enum PopupLocation : byte
{
    Entity,
    Coordinates
}
