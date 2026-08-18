using Content.Shared.Administration.Smites;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Player;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminSmiteSystem
{
    [SubscribeLocalEvent]
    private void OnAddAction(Entity<MetaDataComponent> entity, ref SmiteOperationEvent<AddActionSmite> args)
    {
        foreach (var action in _actions.GetActions(entity))
        {
            if (args.Operation.Action.Equals(MetaData(action).EntityPrototype?.ID))
                return;
        }

        _actions.AddAction(entity, args.Operation.Action);
    }

    [SubscribeLocalEvent]
    private void OnAddMindRole(Entity<MetaDataComponent> entity, ref SmiteOperationEvent<AddMindRoleSmite> args)
    {
        if (!_mind.TryGetMind(entity, out var mindId, out var mind))
            return;

        foreach (var role in mind.MindRoleContainer.ContainedEntities)
        {
            if (args.Operation.Role.Equals(MetaData(role).EntityPrototype?.ID))
                return;
        }

        _role.MindAddRole(mindId, args.Operation.Role, mind);
    }

    [SubscribeLocalEvent]
    private void OnAddUserInterfaces(Entity<MetaDataComponent> entity,
        ref SmiteOperationEvent<AddUserInterfacesSmite> args)
    {
        var userInterface = EnsureComp<UserInterfaceComponent>(entity);

        foreach (var (key, data) in args.Operation.Interfaces)
        {
            _ui.SetUi((entity.Owner, userInterface), key, data);
        }
    }

    [SubscribeLocalEvent]
    private void OnGhostKick(Entity<ActorComponent> entity, ref SmiteOperationEvent<GhostKickSmite> args)
    {
        _ghostKick.DoDisconnect(entity.Comp.PlayerSession.Channel, "Smitten.");
    }

    [SubscribeLocalEvent]
    private void OnSiliconLawBound(Entity<MetaDataComponent> entity,
        ref SmiteOperationEvent<SiliconLawBoundSmite> args)
    {
        EnsureComp<SiliconLawBoundComponent>(entity);

        _siliconLaws.GetLaws(entity.Owner);
        var provider = Comp<SiliconLawProviderComponent>(entity);
        _siliconLaws.NotifyLawsChanged((entity.Owner, provider));
    }
}
