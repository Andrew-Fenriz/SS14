using Content.Shared.Administration.Smites;
using Content.Shared.Movement.Components;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminSmiteSystem
{
    [SubscribeLocalEvent]
    private void OnSwapMovementSpeeds(Entity<MetaDataComponent> entity,
        ref SmiteOperationEvent<SwapMovementSpeedsSmite> args)
    {
        var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(entity);
        (movementSpeed.BaseSprintSpeed, movementSpeed.BaseWalkSpeed) =
            (movementSpeed.BaseWalkSpeed, movementSpeed.BaseSprintSpeed);

        Dirty(entity, movementSpeed);
    }
}
