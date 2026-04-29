using Content.Server.Construction.Components;
using Content.Shared.Database;
using Content.Shared.Kitchen.Components;
using Robust.Shared.Random;

namespace Content.Server.Kitchen.EntitySystems;

public sealed partial class KitchenDeviceSystem
{
    /// <summary>
    /// Triggers an explosion and breaks the device. Logs the incident.
    /// </summary>
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

    /// <summary>
    /// Rolls for random malfunctions during cooking (explosion or lightning).
    /// Called periodically while the device is active.
    /// </summary>
    public void RollMalfunction<TComp>(Entity<ActiveKitchenDeviceComponent, TComp> ent,
        float explosionChance,
        float lightningChance,
        float malfunctionInterval,
        string malfunctionSpark = "Spark")
        where TComp : IComponent
    {
        if (ent.Comp1.MalfunctionTime == TimeSpan.Zero) return;

        if (ent.Comp1.MalfunctionTime > _timing.CurTime) return;

        ent.Comp1.MalfunctionTime = _timing.CurTime + TimeSpan.FromSeconds(malfunctionInterval);

        if (_random.Prob(explosionChance))
        {
            Explode(ent.Owner);
            return;
        }

        if (_random.Prob(lightningChance))
            _lightning.ShootRandomLightnings(ent, 1.0f, 2, malfunctionSpark, triggerLightningEvents: false);
    }
}
