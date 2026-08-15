using System.Numerics;
using Content.Server.Physics.Components;
using Content.Shared.Administration.Smites;
using Content.Shared.Movement.Components;
using Robust.Shared.Random;
using Robust.Shared.Spawners;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminSmiteSystem
{
    [SubscribeLocalEvent]
    private void OnAddComponents(Entity<MetaDataComponent> entity,
        ref SmiteOperationEvent<AddComponentsSmite> args)
    {
        EntityManager.AddComponents(entity, args.Operation.Components, removeExisting: args.Operation.ReplaceExisting);
    }

    [SubscribeLocalEvent]
    private void OnEntityEffects(Entity<MetaDataComponent> entity,
        ref SmiteOperationEvent<EntityEffectsSmite> args)
    {
        _entityEffects.ApplyEffects(entity, args.Operation.Effects, user: args.User);
    }

    [SubscribeLocalEvent]
    private void OnHomingRod(Entity<MetaDataComponent> entity, ref SmiteOperationEvent<HomingRodSmite> args)
    {
        var speed = args.Operation.Speed;
        if (args.Operation.MatchTargetSprintSpeed &&
            TryComp<MovementSpeedModifierComponent>(entity, out var movement))
        {
            speed = movement.CurrentSprintSpeed + 0.001f;
        }

        IRobustRandom random = new RobustRandom();
        random.SetSeed(entity.Owner.Id);
        var offset = random.NextAngle().RotateVec(new Vector2(args.Operation.Distance, 0));
        var spawnCoords = _transform.GetMapCoordinates(entity).Offset(offset);
        var rod = Spawn(args.Operation.Prototype, spawnCoords);

        EnsureComp<ChasingWalkComponent>(rod, out var chasing);
        chasing.NextChangeVectorTime = TimeSpan.MaxValue;
        chasing.ChasingEntity = entity.Owner;
        chasing.ImpulseInterval = 0.1f;
        chasing.RotateWithImpulse = true;
        chasing.MaxSpeed = speed;
        chasing.Speed = speed;

        if (TryComp<TimedDespawnComponent>(rod, out var despawn))
            despawn.Lifetime = offset.Length() / speed * 3;
    }

    [SubscribeLocalEvent]
    private void OnPolymorph(Entity<MetaDataComponent> entity, ref SmiteOperationEvent<PolymorphSmite> args)
    {
        _polymorph.PolymorphEntity(entity, args.Operation.Prototype);
    }
}
