using Content.Server.Construction.Components;
using Content.Shared.Database;
using Content.Shared.Kitchen.Components;
using Robust.Shared.Random;

namespace Content.Server.Kitchen.EntitySystems;

public sealed partial class KitchenDeviceSystem
{
    public void Explode(EntityUid uid, Action? breakDevice = null)
    {
        breakDevice?.Invoke();
        _explosion.TriggerExplosive(uid);

        if (TryComp<MachineComponent>(uid, out var machine))
        {
            CleanContainer(machine.BoardContainer);
            EjectAll(machine.PartContainer);
        }

        _adminLogger.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(uid)} exploded from unsafe cooking!");
    }

    public void RollMalfunction(Entity<MalfunctionComponent> ent)
    {
        if (ent.Comp.NextCheckTime == TimeSpan.Zero) return;

        if (ent.Comp.NextCheckTime > _timing.CurTime) return;

        ent.Comp.NextCheckTime = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.CheckInterval);
        Dirty(ent);

        if (_random.Prob(ent.Comp.ExplosionChance))
        {
            Explode(ent.Owner);
            return;
        }

        if (_random.Prob(ent.Comp.LightningChance))
            _lightning.ShootRandomLightnings(ent, 1.0f, 2, ent.Comp.SparkPrototype, triggerLightningEvents: false);
    }

    public void EnableMalfunctionChecking(EntityUid uid)
    {
        if (!TryComp<MalfunctionComponent>(uid, out var malfunction))
            return;

        malfunction.NextCheckTime = _timing.CurTime + TimeSpan.FromSeconds(malfunction.CheckInterval);
        Dirty(uid, malfunction);
    }

    public void DisableMalfunctionChecking(EntityUid uid)
    {
        if (!TryComp<MalfunctionComponent>(uid, out var malfunction))
            return;

        malfunction.NextCheckTime = TimeSpan.Zero;
        Dirty(uid, malfunction);
    }
}
