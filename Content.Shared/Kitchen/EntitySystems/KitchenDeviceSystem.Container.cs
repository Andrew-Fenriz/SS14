using System.Linq;
using Content.Shared.Item;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Kitchen.EntitySystems;

public partial class KitchenDeviceSystem
{
    public Container EnsureContainer(EntityUid uid, string containerId)
    {
        return _container.EnsureContainer<Container>(uid, containerId);
    }

    public static bool HasContents(Container container)
    {
        return container.ContainedEntities.Any();
    }

    private static bool IsFull(Container container, int capacity)
    {
        return container.ContainedEntities.Count >= capacity;
    }

    public void EjectAll(Container container)
    {
        _container.EmptyContainer(container);
    }

    protected void EjectEntity(EntityUid entity, Container container)
    {
        _container.Remove(entity, container);
    }

    private void InsertEntity(EntityUid entity, Container container)
    {
        _container.Insert(entity, container);
    }

    protected void CleanContainer(Container container)
    {
        _container.CleanContainer(container);
    }

    public bool ItemFitsInDevice(Container container, int capacity, EntityUid item, string maxItemSize)
    {
        if (IsFull(container, capacity))
            return false;

        if (!TryComp<ItemComponent>(item, out var itemComp))
            return false;

        var maxSize = _item.GetSizePrototype(maxItemSize);
        var itemSize = _item.GetSizePrototype(itemComp.Size);
        return itemSize <= maxSize;
    }

    public bool CanInsertItem(EntityUid uid, EntityUid item, Container container, int capacity,
        EntityWhitelist? whitelist = null, bool isBroken = false, bool isOperating = false)
    {
        if (isBroken)
            return false;

        if (isOperating)
            return false;

        if (IsFull(container, capacity))
            return false;

        return whitelist == null || _whitelist.IsValid(whitelist, item);
    }

    private static void ProcessContainerContents<TContext>(Container container, IEntityManager entityManager, Func<EntityUid, TContext, bool> processor, TContext context)
    {
        var snapshot = container.ContainedEntities.Where(entityManager.EntityExists).ToArray();
        foreach (var item in snapshot)
        {
            if (!item.IsValid())
                continue;

            if (!processor(item, context))
                break;
        }
    }

    public void ProcessContainerContents<TContext>(Container container, Func<EntityUid, TContext, bool> processor, TContext context)
    {
        ProcessContainerContents(container, EntityManager, processor, context);
    }

    private static void ProcessContainerContents(Container container, IEntityManager entityManager, Func<EntityUid, bool> processor)
    {
        var snapshot = container.ContainedEntities.Where(entityManager.EntityExists).ToArray();
        foreach (var item in snapshot)
        {
            if (!item.IsValid())
                continue;

            if (!processor(item))
                break;
        }
    }

    public void ProcessContainerContents(Container container, Func<EntityUid, bool> processor)
    {
        ProcessContainerContents(container, EntityManager, processor);
    }

    public void ReplaceEntityWithJunk(EntityUid device, EntityUid item, Container container, EntProtoId? junkPrototypeId)
    {
        if (!junkPrototypeId.HasValue)
            return;

        if (!container.Contains(item))
            return;

        var coords = Transform(device).Coordinates;
        var junk = Spawn(junkPrototypeId.Value, coords);

        EjectEntity(item, container);
        Del(item);
        InsertEntity(junk, container);
    }
}
