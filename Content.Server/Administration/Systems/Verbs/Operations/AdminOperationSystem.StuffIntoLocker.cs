using Content.Shared.Administration.Verbs.Operations;
using Content.Shared.Administration.Verbs.Operations.Smites;
using Content.Shared.Storage.Components;

namespace Content.Server.Administration.Systems.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnStuffIntoLocker(Entity<MetaDataComponent> entity,
        ref AdminOperationEvent<StuffIntoLockerOperation> args)
    {
        var locker = Spawn(args.Operation.Prototype, Transform(entity).Coordinates);

        if (TryComp<EntityStorageComponent>(locker, out var storage))
        {
            // Insert on an open entity storage drops the target beside it. Closing it then
            // captures nearby eligible entities, preserving the original stuffing behavior.
            _entityStorage.ToggleOpen(entity.Owner, locker, storage);
            _entityStorage.Insert(entity.Owner, locker, storage);
            _entityStorage.ToggleOpen(entity.Owner, locker, storage);
        }

        _weldable.SetWeldedState(locker, true);
    }
}
