using Content.Shared.Administration.Smites;
using Robust.Shared.Player;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminSmiteSystem
{
    [SubscribeLocalEvent]
    private void OnGhostKick(Entity<ActorComponent> entity, ref SmiteOperationEvent<GhostKickSmite> args)
    {
        _ghostKick.DoDisconnect(entity.Comp.PlayerSession.Channel, "Smitten.");
    }
}
