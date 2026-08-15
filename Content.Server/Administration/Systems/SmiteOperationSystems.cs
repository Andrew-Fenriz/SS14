using Content.Server.Polymorph.Systems;
using Content.Shared.Administration.Smites;
using Content.Shared.Administration.Smites.Operations;
using Content.Shared.EntityEffects;

namespace Content.Server.Administration.Systems;

/// <summary>
/// Applies the entity effects configured by an <see cref="Shared.Administration.Smites.Operations.ApplyEntityEffectsSmiteOperation"/> smite operation.
/// </summary>
public sealed partial class ApplyEntityEffectsSmiteOperationSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _entityEffects = default!;

    [SubscribeLocalEvent]
    private void OnOperation(Entity<MetaDataComponent> entity,
        ref SmiteOperationEvent<ApplyEntityEffectsSmiteOperation> args)
    {
        _entityEffects.ApplyEffects(entity, args.Operation.Effects, user: args.User);
    }
}

/// <summary>
/// Polymorphs targets for <see cref="PolymorphSmiteOperation"/> smite operations.
/// </summary>
public sealed partial class PolymorphSmiteOperationSystem : EntitySystem
{
    [Dependency] private PolymorphSystem _polymorph = default!;

    [SubscribeLocalEvent]
    private void OnOperation(Entity<MetaDataComponent> entity, ref SmiteOperationEvent<PolymorphSmiteOperation> args)
    {
        _polymorph.PolymorphEntity(entity, args.Operation.Prototype);
    }
}
