using Content.Server.Animals.Components;
using Content.Server.Popups;
using Content.Shared.IdentityManagement;
using Robust.Server.Audio;

namespace Content.Server.Animals.Systems;

/// <summary>
/// Handles optional feedback for production attempts.
/// </summary>
public sealed partial class ProductionFeedbackSystem : EntitySystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private PopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnSatiationProductionFailed(Entity<ProductionFeedbackComponent> ent, ref SatiationProductionFailedEvent args)
    {
        if (args.Requester is not { } requester ||
            args.Failure != SatiationProductionFailure.InsufficientSatiation ||
            ent.Comp.InsufficientSatiationPopup is not { } popup)
            return;

        _popup.PopupEntity(
            Loc.GetString(popup),
            requester,
            requester);
    }

    [SubscribeLocalEvent]
    private void OnProductionCompleted(Entity<ProductionFeedbackComponent> ent, ref ProductionCompletedEvent args)
    {
        if (ent.Comp.ProductionSound is { } sound)
            _audio.PlayPvs(sound, args.Producer);

        var userPopup = ent.Comp.UserPopup is { } user
            ? Loc.GetString(user)
            : null;

        var othersPopup = ent.Comp.OthersPopup is { } others
            ? Loc.GetString(others, ("entity", Identity.Entity(args.Producer, EntityManager)))
            : null;

        if (userPopup == null && othersPopup == null)
            return;

        _popup.PopupEntity(
            userPopup,
            othersPopup,
            args.Producer,
            args.Producer);
    }
}
