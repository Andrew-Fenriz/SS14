using Content.Shared.Administration.Verbs.Operations;
using Content.Shared.Administration.Verbs.Operations.Smites;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Server.Administration.Systems.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnPinball(Entity<PhysicsComponent> entity, ref AdminOperationEvent<PinballOperation> args)
    {
        if (!TryComp<FixturesComponent>(entity, out var fixtures))
            return;

        PreparePhysicsTarget(entity, fixtures);

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
}
