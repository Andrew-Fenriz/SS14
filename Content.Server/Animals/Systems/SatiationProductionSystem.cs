using Content.Server.Animals.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Animals.Systems;

/// <inheritdoc cref="SatiationProductionComponent"/>
public sealed partial class SatiationProductionSystem : EntitySystem
{
    [Dependency] private ProductionSystem _production = default!;
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IRobustRandom _random = default!;

    [Dependency] private EntityQuery<ActorComponent> _actorQuery;
    [Dependency] private EntityQuery<SatiationComponent> _satiationQuery;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SatiationProductionComponent>();
        while (query.MoveNext(out var uid, out var producer))
        {
            if (!producer.Automatic)
                continue;

            var producerUid = GetProducer((uid, producer));
            if (!producer.AutomaticForPlayers && _actorQuery.HasComp(producerUid))
                continue;

            if (_timing.CurTime < producer.NextProductionTime)
                continue;

            producer.NextProductionTime += GetDelay(producer);
            _production.TryProduce(uid, producerUid);
        }
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<SatiationProductionComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextProductionTime = _timing.CurTime + GetDelay(ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnBeforeProduction(
        Entity<SatiationProductionComponent> ent,
        ref BeforeProductionEvent args)
    {
        if (args.Cancelled)
            return;

        var failure = GetFailure(ent.Comp, args.Producer);
        if (failure == SatiationProductionFailure.None)
            return;

        args.Cancelled = true;

        var ev = new SatiationProductionFailedEvent(
            args.Producer,
            args.Requester,
            failure);

        RaiseLocalEvent(ent.Owner, ref ev);
    }

    [SubscribeLocalEvent]
    private void OnProductionCompleted(
        Entity<SatiationProductionComponent> ent,
        ref ProductionCompletedEvent args)
    {
        if (!_satiationQuery.TryComp(args.Producer, out var satiation) ||
            !satiation.Has(ent.Comp.SatiationType))
        {
            return;
        }

        _satiation.ModifyValue(
            (args.Producer, satiation),
            ent.Comp.SatiationType,
            -ent.Comp.SatiationUsage);
    }

    private SatiationProductionFailure GetFailure(
        SatiationProductionComponent component,
        EntityUid producer)
    {
        if (_mobState.IsDead(producer))
            return SatiationProductionFailure.Dead;

        if (_satiationQuery.TryComp(producer, out var satiation) &&
            satiation.Has(component.SatiationType) &&
            !HasEnoughSatiation(component, (producer, satiation)))
        {
            return SatiationProductionFailure.InsufficientSatiation;
        }

        return SatiationProductionFailure.None;
    }

    private bool HasEnoughSatiation(
        SatiationProductionComponent component,
        Entity<SatiationComponent> satiation)
    {
        if (component.MinimumSatiationThreshold is { } threshold &&
            !_satiation.IsValueInRange(
                satiation,
                component.SatiationType,
                above: threshold,
                hypotheticalValueDelta: -component.SatiationUsage))
        {
            return false;
        }

        return component.MinimumSatiation is not { } minimum ||
               _satiation.GetValueOrNull(satiation, component.SatiationType) >= minimum;
    }

    private EntityUid GetProducer(Entity<SatiationProductionComponent> ent)
    {
        return ent.Comp.Producer switch
        {
            SatiationProductionOwner.Parent => Transform(ent).ParentUid,
            _ => ent.Owner
        };
    }

    private TimeSpan GetDelay(SatiationProductionComponent component)
    {
        if (component.DelayMax is not { } maximum)
            return component.Delay;

        var seconds = _random.NextDouble(component.Delay.TotalSeconds, maximum.TotalSeconds);
        return TimeSpan.FromSeconds(seconds);
    }
}
