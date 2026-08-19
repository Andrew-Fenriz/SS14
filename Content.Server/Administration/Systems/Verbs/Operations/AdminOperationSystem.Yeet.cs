using Content.Shared.Administration.Verbs.Operations;
using Content.Shared.Administration.Verbs.Operations.Smites;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Server.Administration.Systems.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnYeet(Entity<PhysicsComponent> entity, ref AdminOperationEvent<YeetOperation> args)
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
}
