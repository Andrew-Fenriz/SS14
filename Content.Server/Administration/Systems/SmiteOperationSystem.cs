using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Shared.Administration.Smites;
using Content.Shared.Administration.Smites.Operations;
using Content.Shared.Body;
using Content.Shared.Clothing.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Robust.Shared.Player;

namespace Content.Server.Administration.Systems;

/// <summary>
/// Handles the concrete operations performed by declarative admin smites.
/// </summary>
public sealed partial class SmiteOperationSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private PolymorphSystem _polymorph = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void OnAddComponents(Entity<MetaDataComponent> entity,
        ref SmiteOperationEvent<AddComponentsSmite> args)
    {
        EntityManager.AddComponents(entity, args.Operation.Components, removeExisting: args.Operation.ReplaceExisting);
    }

    [SubscribeLocalEvent]
    private void OnEntityEffects(Entity<MetaDataComponent> entity,
        ref SmiteOperationEvent<EntityEffectsSmite> args)
    {
        _entityEffects.ApplyEffects(entity, args.Operation.Effects, user: args.User);
    }

    [SubscribeLocalEvent]
    private void OnPolymorph(Entity<MetaDataComponent> entity, ref SmiteOperationEvent<PolymorphSmite> args)
    {
        _polymorph.PolymorphEntity(entity, args.Operation.Prototype);
    }

    [SubscribeLocalEvent]
    private void OnPopup(Entity<MetaDataComponent> entity, ref SmiteOperationEvent<PopupSmite> args)
    {
        var message = Loc.GetString(args.Operation.Message,
            ("name", entity.Owner),
            ("entity", entity.Owner));

        switch ((args.Operation.Recipients, args.Operation.Location))
        {
            case (SmitePopupRecipients.Target, SmitePopupLocation.Entity):
                _popup.PopupEntity(message, entity, entity, args.Operation.Type);
                break;
            case (SmitePopupRecipients.Target, SmitePopupLocation.Coordinates):
                _popup.PopupCoordinates(message, Transform(entity).Coordinates, entity, args.Operation.Type);
                break;
            case (SmitePopupRecipients.Pvs, SmitePopupLocation.Entity):
                _popup.PopupEntity(message, entity, args.Operation.Type);
                break;
            case (SmitePopupRecipients.Pvs, SmitePopupLocation.Coordinates):
                _popup.PopupCoordinates(message, Transform(entity).Coordinates, args.Operation.Type);
                break;
            case (SmitePopupRecipients.PvsExceptTarget, SmitePopupLocation.Entity):
                _popup.PopupEntity(message, entity, Filter.PvsExcept(entity), true, args.Operation.Type);
                break;
            case (SmitePopupRecipients.PvsExceptTarget, SmitePopupLocation.Coordinates):
                _popup.PopupCoordinates(
                    message,
                    Transform(entity).Coordinates,
                    Filter.PvsExcept(entity),
                    true,
                    args.Operation.Type);
                break;
        }
    }

    [SubscribeLocalEvent]
    private void OnRemoveOrgans(Entity<BodyComponent> entity,
        ref SmiteOperationEvent<RemoveOrgansSmite> args)
    {
        if (args.Operation.MaxCount is <= 0)
            return;

        var selected = new List<EntityUid>();
        foreach (var organ in _body.EnumerateOrgans<TransformComponent>(entity.AsNullable()))
        {
            var category = organ.Comp1.Category;
            if (args.Operation.Categories != null &&
                (category == null || !args.Operation.Categories.Contains(category.Value)))
            {
                continue;
            }

            if (category != null && args.Operation.ExcludedCategories.Contains(category.Value))
                continue;

            selected.Add(organ);
            if (selected.Count == args.Operation.MaxCount)
                break;
        }

        foreach (var organ in selected)
        {
            _transform.AttachToGridOrMap(organ);
        }
    }

    [SubscribeLocalEvent]
    private void OnSetEquipment(Entity<InventoryComponent> entity,
        ref SmiteOperationEvent<SetEquipmentSmite> args)
    {
        if (args.Operation.ClearOtherSlots && _inventory.TryGetSlots(entity, out var slots))
        {
            foreach (var slot in slots)
            {
                _inventory.TryUnequip(entity, slot.Name, silent: true, force: true, inventory: entity.Comp);
            }
        }

        foreach (var (slot, prototype) in args.Operation.Equipment)
        {
            if (!args.Operation.ClearOtherSlots)
                _inventory.TryUnequip(entity, slot, silent: true, force: true, inventory: entity.Comp);

            var equipment = Spawn(prototype, Transform(entity).Coordinates);
            if (!_inventory.TryEquip(entity, equipment, slot, silent: true, force: true, inventory: entity.Comp))
            {
                QueueDel(equipment);
                continue;
            }

            if (args.Operation.Unremoveable && HasComp<ClothingComponent>(equipment))
                EnsureComp<UnremoveableComponent>(equipment);
        }
    }
}
