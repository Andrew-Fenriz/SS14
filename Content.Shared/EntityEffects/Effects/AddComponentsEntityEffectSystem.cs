using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Adds a configured set of components to an entity.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
public sealed partial class AddComponentsEntityEffectSystem : EntityEffectSystem<MetaDataComponent, AddComponents>
{
    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<AddComponents> args)
    {
        EntityManager.AddComponents(entity, args.Effect.Components, args.Effect.RemoveExisting);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class AddComponents : EntityEffectBase<AddComponents>
{
    /// <summary>
    /// Components to add to the affected entity.
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    /// <summary>
    /// Whether components that already exist on the entity should be replaced.
    /// </summary>
    [DataField]
    public bool RemoveExisting;
}
