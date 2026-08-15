using Content.Shared.EntityEffects;

namespace Content.Shared.Administration.Smites.Operations;

/// <summary>
/// Applies a configured set of entity effects to the smite target.
/// </summary>
public sealed partial class EntityEffectsSmite : SmiteOperationBase<EntityEffectsSmite>
{
    [DataField(required: true)]
    public EntityEffect[] Effects { get; private set; } = [];
}
