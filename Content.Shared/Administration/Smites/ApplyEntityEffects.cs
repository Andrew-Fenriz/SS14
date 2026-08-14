using Content.Shared.EntityEffects;

namespace Content.Shared.Administration.Smites;

/// <summary>
/// Applies a configured set of entity effects to the smite target.
/// </summary>
public sealed partial class ApplyEntityEffects : SmiteOperationBase<ApplyEntityEffects>
{
    [DataField(required: true)]
    public EntityEffect[] Effects { get; private set; } = [];
}
