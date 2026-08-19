using Content.Shared.EntityEffects;

namespace Content.Shared.Administration.Verbs.Operations;

/// <summary>
/// Applies a configured set of entity effects to the target.
/// </summary>
public sealed partial class EntityEffectsOperation : AdminOperationBase<EntityEffectsOperation>
{
    [DataField(required: true)]
    public EntityEffect[] Effects { get; private set; } = [];
}
