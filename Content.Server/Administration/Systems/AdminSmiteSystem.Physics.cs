using Content.Shared.Administration.Smites;
using Content.Shared.Friction;
using Content.Shared.Movement.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminSmiteSystem
{
    [SubscribeLocalEvent]
    private void OnPinball(Entity<PhysicsComponent> entity, ref SmiteOperationEvent<PinballSmite> args)
    {
        if (!TryComp<FixturesComponent>(entity, out var fixtures))
            return;

        PreparePhysicsSmite(entity, fixtures);

        foreach (var fixture in fixtures.Fixtures.Values)
        {
            if (!fixture.Hard)
                continue;

            _physics.SetRestitution(entity, fixture, 1.1f, false, fixtures);
        }

        _fixtures.FixtureUpdate(entity, manager: fixtures, body: entity.Comp);
        _physics.SetLinearVelocity(entity, _random.NextVector2(1.5f, 1.5f), manager: fixtures, body: entity.Comp);
        _physics.SetAngularVelocity(entity, MathF.PI * 12, manager: fixtures, body: entity.Comp);
    }

    [SubscribeLocalEvent]
    private void OnSwapMovementSpeeds(Entity<MetaDataComponent> entity,
        ref SmiteOperationEvent<SwapMovementSpeedsSmite> args)
    {
        var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(entity);
        (movementSpeed.BaseSprintSpeed, movementSpeed.BaseWalkSpeed) =
            (movementSpeed.BaseWalkSpeed, movementSpeed.BaseSprintSpeed);

        Dirty(entity, movementSpeed);
    }

    [SubscribeLocalEvent]
    private void OnYeet(Entity<PhysicsComponent> entity, ref SmiteOperationEvent<YeetSmite> args)
    {
        if (!TryComp<FixturesComponent>(entity, out var fixtures))
            return;

        PreparePhysicsSmite(entity, fixtures);

        foreach (var fixture in fixtures.Fixtures.Values)
        {
            _physics.SetHard(entity, fixture, false, manager: fixtures);
        }

        _physics.SetLinearVelocity(entity, _random.NextVector2(8f, 8f), manager: fixtures, body: entity.Comp);
        _physics.SetAngularVelocity(entity, MathF.PI * 12, manager: fixtures, body: entity.Comp);
    }

    private void PreparePhysicsSmite(Entity<PhysicsComponent> entity, FixturesComponent fixtures)
    {
        _transform.Unanchor(entity);
        _physics.SetBodyType(entity, BodyType.Dynamic, manager: fixtures, body: entity.Comp);
        _physics.SetBodyStatus(entity, entity.Comp, BodyStatus.InAir);
        _physics.WakeBody(entity, manager: fixtures, body: entity.Comp);

        EnsureComp<TileFrictionModifierComponent>(entity, out var friction);
        _tileFriction.SetModifier(entity, 0f, friction);
    }
}
