using System.Linq;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Unit;
using Content.Shared.Storage;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

public static class CleanbotConfig
{
    public const string DefaultTargetKey = "DisposalUnit";
}

/// <summary>
/// Empties all trash from the bot's internal storage into a DisposalUnit.
/// </summary>
public sealed partial class TrashbotDisposeOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private SharedAudioSystem _audioSystem = default!;
    private SharedContainerSystem _containerSystem = default!;
    private SharedDisposalUnitSystem _disposalUnitSystem = default!;

    [DataField("dumpSound")]
    public SoundSpecifier? DumpSound = new SoundPathSpecifier("/Audio/Effects/trashbag1.ogg");

    [DataField("targetKey", required: true)]
    public string TargetKey = CleanbotConfig.DefaultTargetKey;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _containerSystem = sysManager.GetEntitySystem<SharedContainerSystem>();
        _audioSystem = sysManager.GetEntitySystem<SharedAudioSystem>();
        _disposalUnitSystem = sysManager.GetEntitySystem<SharedDisposalUnitSystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.Owner, out var owner, _entManager))
            return HTNOperatorStatus.Failed;

        if (!_entManager.TryGetComponent<StorageComponent>(owner, out var storage) ||
            storage.Container.ContainedEntities.Count == 0)
            return HTNOperatorStatus.Finished;

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var disposalUnit, _entManager))
            return HTNOperatorStatus.Failed;

        if (!_entManager.HasComponent<DisposalUnitComponent>(disposalUnit) ||
            _entManager.IsQueuedForDeletion(disposalUnit))
            return HTNOperatorStatus.Failed;

        if (!_entManager.TryGetComponent(disposalUnit, out ContainerManagerComponent? disposalMgr))
            return HTNOperatorStatus.Failed;

        var disposalContainer = disposalMgr.Containers.FirstOrDefault().Value;
        if (disposalContainer == null)
            return HTNOperatorStatus.Failed;

        var movedAny = false;

        foreach (var item in storage.Container.ContainedEntities.ToArray())
        {
            if (!_containerSystem.TryRemoveFromContainer(item))
                continue;

            if (_containerSystem.Insert(item, disposalContainer))
                movedAny = true;
        }

        if (movedAny && DumpSound != null)
            _audioSystem.PlayPredicted(DumpSound, owner, owner);

        return movedAny ? HTNOperatorStatus.Finished : HTNOperatorStatus.Failed;
    }
}
