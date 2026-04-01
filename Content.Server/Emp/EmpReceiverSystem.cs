using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Emp;

namespace Content.Server.Emp;

public sealed class EmpReceiverSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmpReceiverComponent, EmpPulseEvent>(OnEmpPulse);
    }

    private void OnEmpPulse(Entity<EmpReceiverComponent> ent, ref EmpPulseEvent args)
    {
        args.Affected = true;
        args.Disabled = true;

        var damage = ent.Comp.Damage;

        if (ent.Comp.ScaleByEnergy)
            damage *= args.EnergyConsumption;

        if (damage <= 0)
            return;

        var dmg = new DamageSpecifier();
        dmg.DamageDict.Add(ent.Comp.DamageType, damage);

        _damageable.TryChangeDamage(ent.Owner, dmg);
    }
}