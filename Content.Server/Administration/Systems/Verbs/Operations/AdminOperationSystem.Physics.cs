using Content.Shared.Friction;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Server.Administration.Systems.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    private void PreparePhysicsTarget(Entity<PhysicsComponent> entity, FixturesComponent fixtures)
    {
        _transform.Unanchor(entity);
        _physics.SetBodyType(entity, BodyType.Dynamic, manager: fixtures, body: entity.Comp);
        _physics.SetBodyStatus(entity, entity.Comp, BodyStatus.InAir);
        _physics.WakeBody(entity, manager: fixtures, body: entity.Comp);

        EnsureComp<TileFrictionModifierComponent>(entity, out var friction);
        _tileFriction.SetModifier(entity, 0f, friction);
    }
}
