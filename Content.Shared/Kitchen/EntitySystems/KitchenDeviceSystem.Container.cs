using System.Linq;
using Content.Shared.Item;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Kitchen.EntitySystems;

public partial class KitchenDeviceSystem
{
    /// <summary>
    /// Gets or creates a container on the entity.
    /// </summary>
    public Container EnsureContainer(EntityUid uid, string containerId)
    {
        return _container.EnsureContainer<Container>(uid, containerId);
    }

    /// <summary>
    /// Checks if the container has any items.
    /// </summary>
    public static bool HasContents(Container container)
    {
        return container.ContainedEntities.Any();
    }

    private static bool IsFull(Container container, int capacity)
    {
        return container.ContainedEntities.Count >= capacity;
    }

    /// <summary>
    /// Removes all items from the container and places them near the entity.
    /// </summary>
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

    /// <summary>
    /// Destroys all items inside the container.
    /// </summary>
    protected void CleanContainer(Container container)
    {
        _container.CleanContainer(container);
    }

    /// <summary>
    /// Checks if an item fits based on container capacity and item size limits.
    /// </summary>
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

    /// <summary>
    /// Validates if an item can be inserted into a kitchen device.
    /// Checks device state, capacity, and whitelist.
    /// </summary>
    public bool CanInsertItem(EntityUid uid, EntityUid item, Container container, int capacity,
        EntityWhitelist? whitelist = null, bool isBroken = false)
    {
        if (isBroken)
            return false;

        if (IsActive(uid))
            return false;

        if (IsFull(container, capacity))
            return false;

        return whitelist == null || _whitelist.IsValid(whitelist, item);
    }

    /// <summary>
    /// Processes each item in the container with a callback function.
    /// Automatically handles collection modification during iteration.
    /// </summary>
    public static void ProcessContainerContents<TContext>(Container container, Func<EntityUid, TContext, bool> processor, TContext context)
    {
        foreach (var item in container.ContainedEntities.ToArray())
        {
            if (!processor(item, context))
                break;
        }
    }

    /// <summary>
    /// Processes each item in the container with a simple callback (no context).
    /// Automatically handles collection modification during iteration.
    /// </summary>
    public static void ProcessContainerContents(Container container, Func<EntityUid, bool> processor)
    {
        foreach (var item in container.ContainedEntities.ToArray())
        {
            if (!processor(item))
                break;
        }
    }

    /// <summary>
    /// Replaces an item with a "junk" or "burned" result (used for failed recipes).
    /// Spawns the junk entity and swaps it in the container.
    /// </summary>
    public void ReplaceEntityWithJunk(EntityUid device, EntityUid item, Container container, EntProtoId? junkPrototypeId)
    {
        if (!junkPrototypeId.HasValue)
            return;

        if (!container.Contains(item))
            return;

        var coords = Transform(device).Coordinates;
        var junk = Spawn(junkPrototypeId.Value, coords);

        // Remove old item and insert junk
        EjectEntity(item, container);
        Del(item);
        InsertEntity(junk, container);
    }
}
