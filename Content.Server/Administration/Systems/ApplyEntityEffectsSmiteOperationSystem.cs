using Content.Shared.Administration.Smites;
using Content.Shared.EntityEffects;

namespace Content.Server.Administration.Systems;

/// <summary>
/// Applies the entity effects configured by an <see cref="ApplyEntityEffects"/> smite operation.
/// </summary>
public sealed partial class ApplyEntityEffectsSmiteOperationSystem
    : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _entityEffects = default!;

    [SubscribeLocalEvent]
    private void OnOperation(Entity<MetaDataComponent> entity, ref SmiteOperationEvent<ApplyEntityEffects> args)
    {
        _entityEffects.ApplyEffects(entity, args.Operation.Effects, user: args.User);
    }
}
