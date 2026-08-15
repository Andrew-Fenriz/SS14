using Content.Server.Polymorph.Systems;
using Content.Shared.Administration.Smites;
using Content.Shared.Administration.Smites.Operations;
using Content.Shared.Body;
using Content.Shared.EntityEffects;

namespace Content.Server.Administration.Systems;

/// <summary>
/// Handles the concrete operations performed by declarative admin smites.
/// </summary>
public sealed partial class SmiteOperationSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private PolymorphSystem _polymorph = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void OnAddComponents(Entity<MetaDataComponent> entity,
        ref SmiteOperationEvent<AddComponentsSmite> args)
    {
        EntityManager.AddComponents(entity, args.Operation.Components, removeExisting: false);
    }

    [SubscribeLocalEvent]
    private void OnEntityEffects(Entity<MetaDataComponent> entity,
        ref SmiteOperationEvent<EntityEffectsSmite> args)
    {
        _entityEffects.ApplyEffects(entity, args.Operation.Effects, user: args.User);
    }

    [SubscribeLocalEvent]
    private void OnPolymorph(Entity<MetaDataComponent> entity, ref SmiteOperationEvent<PolymorphSmite> args)
    {
        _polymorph.PolymorphEntity(entity, args.Operation.Prototype);
    }

    [SubscribeLocalEvent]
    private void OnRemoveOrgans(Entity<BodyComponent> entity,
        ref SmiteOperationEvent<RemoveOrgansSmite> args)
    {
        if (args.Operation.MaxCount is <= 0)
            return;

        var selected = new List<EntityUid>();
        foreach (var organ in _body.EnumerateOrgans<TransformComponent>(entity.AsNullable()))
        {
            var category = organ.Comp1.Category;
            if (args.Operation.Categories != null &&
                (category == null || !args.Operation.Categories.Contains(category.Value)))
            {
                continue;
            }

            if (category != null && args.Operation.ExcludedCategories.Contains(category.Value))
                continue;

            selected.Add(organ);
            if (selected.Count == args.Operation.MaxCount)
                break;
        }

        foreach (var organ in selected)
        {
            _transform.AttachToGridOrMap(organ);
        }
    }
}
