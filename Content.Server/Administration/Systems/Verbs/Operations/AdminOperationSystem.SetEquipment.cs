using Content.Shared.Administration.Verbs.Operations;
using Content.Shared.Clothing.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;

namespace Content.Server.Administration.Systems.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnSetEquipment(Entity<InventoryComponent> entity, ref AdminOperationEvent<SetEquipmentOperation> args)
    {
        if (args.Operation.StartingGear is { } startingGear)
        {
            _outfit.SetOutfit(
                entity,
                startingGear,
                unremovable: args.Operation.Unremoveable);
        }

        if (args.Operation.ClearOtherSlots &&
            args.Operation.StartingGear == null &&
            _inventory.TryGetSlots(entity, out var slots))
        {
            foreach (var slot in slots)
            {
                _inventory.TryUnequip(entity, slot.Name, true, true, inventory: entity.Comp);
            }
        }

        foreach (var (slot, prototype) in args.Operation.Equipment)
        {
            if (!args.Operation.ClearOtherSlots || args.Operation.StartingGear != null)
                _inventory.TryUnequip(entity, slot, true, true, inventory: entity.Comp);

            var equipment = Spawn(prototype, Transform(entity).Coordinates);
            if (!_inventory.TryEquip(entity, equipment, slot, true, true, inventory: entity.Comp))
            {
                QueueDel(equipment);
                continue;
            }

            if (args.Operation.Unremoveable && HasComp<ClothingComponent>(equipment))
                EnsureComp<UnremoveableComponent>(equipment);
        }
    }
}
