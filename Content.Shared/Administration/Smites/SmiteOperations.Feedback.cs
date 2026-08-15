using Content.Shared.Popups;

namespace Content.Shared.Administration.Smites;

/// <summary>
/// Displays one localized popup for a smite target.
/// </summary>
public sealed partial class PopupSmite : SmiteOperationBase<PopupSmite>
{
    [DataField(required: true)]
    public LocId Message { get; private set; }

    [DataField]
    public SmitePopupRecipients Recipients { get; private set; } = SmitePopupRecipients.Target;

    [DataField]
    public SmitePopupLocation Location { get; private set; } = SmitePopupLocation.Entity;

    [DataField]
    public PopupType Type { get; private set; } = PopupType.Small;
}

public enum SmitePopupRecipients : byte
{
    Target,
    Pvs,
    PvsExceptTarget
}

public enum SmitePopupLocation : byte
{
    Entity,
    Coordinates
}
