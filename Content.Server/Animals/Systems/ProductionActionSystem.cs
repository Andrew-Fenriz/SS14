using Content.Server.Animals.Components;
using Content.Server.Popups;
using Content.Shared.Animals.Events;
using Content.Shared.IdentityManagement;
using Robust.Server.Audio;

namespace Content.Server.Animals.Systems;

/// <summary>
/// Handles production actions and their feedback.
/// </summary>
public sealed partial class ProductionActionSystem : EntitySystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private ProductionSystem _production = default!;

    [SubscribeLocalEvent]
    private void OnProductionAction(
        Entity<ProductionActionComponent> ent,
        ref ProductionActionEvent args)
    {
        var source = args.Source switch
        {
            ProductionActionSource.Action => args.Action.Owner,
            _ => args.Performer
        };

        args.Handled = _production.TryProduce(
            source,
            args.Performer,
            args.Performer);
    }

    [SubscribeLocalEvent]
    private void OnSatiationProductionFailed(
        Entity<EntityProducerActionComponent> ent,
        ref SatiationProductionFailedEvent args)
    {
        if (args.Requester is not { } requester ||
            args.Failure != SatiationProductionFailure.InsufficientSatiation)
        {
            return;
        }

        _popup.PopupEntity(
            Loc.GetString(ent.Comp.InsufficientSatiationPopup),
            requester,
            requester);
    }

    [SubscribeLocalEvent]
    private void OnEntitiesProduced(
        Entity<EntityProducerActionComponent> ent,
        ref EntitiesProducedEvent args)
    {
        _audio.PlayPvs(ent.Comp.ProductionSound, args.Owner);
        _popup.PopupEntity(
            Loc.GetString(ent.Comp.UserPopup),
            Loc.GetString(
                ent.Comp.OthersPopup,
                ("entity", Identity.Entity(args.Owner, EntityManager))),
            args.Owner,
            args.Owner);
    }
}
