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
    private void OnSatiationProductionFailed(
        Entity<ProductionFeedbackComponent> ent,
        ref SatiationProductionFailedEvent args)
    {
        if (args.Requester is not { } requester ||
            args.Failure != SatiationProductionFailure.InsufficientSatiation ||
            ent.Comp.InsufficientSatiationPopup is not { } popup)
        {
            return;
        }

        _popup.PopupEntity(
            Loc.GetString(popup),
            requester,
            requester);
    }

    [SubscribeLocalEvent]
    private void OnProductionCompleted(
        Entity<ProductionFeedbackComponent> ent,
        ref ProductionCompletedEvent args)
    {
        if (ent.Comp.ProductionSound is { } sound)
            _audio.PlayPvs(sound, args.Producer);

        if (ent.Comp.UserPopup is not { } userPopup ||
            ent.Comp.OthersPopup is not { } othersPopup)
        {
            return;
        }

        _popup.PopupEntity(
            Loc.GetString(userPopup),
            Loc.GetString(
                othersPopup,
                ("entity", Identity.Entity(args.Producer, EntityManager))),
            args.Producer,
            args.Producer);
    }
}
