using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Checks whether an entity's storage has available capacity, is full, or has items.
/// </summary>
public sealed partial class StorageCapacityPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private SharedStorageSystem _storageSystem = default!;

    [DataField("checkFull")]
    public bool CheckFull;

    [DataField("checkHasItems")]
    public bool CheckHasItems;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _storageSystem = sysManager.GetEntitySystem<SharedStorageSystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.Owner, out var owner, _entManager))
            return false;

        if (!_entManager.TryGetComponent<StorageComponent>(owner, out var storage))
            return false;

        if (CheckHasItems)
            return storage.Container.ContainedEntities.Count > 0;

        var hasSpace = _storageSystem.HasSpace((owner, storage));
        return CheckFull ? !hasSpace : hasSpace;
    }
}
