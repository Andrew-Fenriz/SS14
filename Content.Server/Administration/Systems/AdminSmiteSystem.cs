using Content.Shared.Administration.Prototypes;
using Content.Shared.Administration.Smites;

namespace Content.Server.Administration.Systems;

/// <summary>
/// Executes the ordered operations belonging to declarative admin smites.
/// </summary>
public sealed partial class AdminSmiteSystem : EntitySystem, ISmiteOperationRaiser
{
    public void Apply(EntityUid target, EntityUid user, AdminSmitePrototype prototype)
    {
        Apply(target, user, prototype.Operations);
    }

    public void Apply(EntityUid target, EntityUid user, SmiteOperation[] operations)
    {
        foreach (var operation in operations)
        {
            operation.RaiseEvent(target, user, this);
        }
    }

    public void RaiseOperationEvent<T>(EntityUid target, EntityUid user, T operation)
        where T : SmiteOperationBase<T>
    {
        var operationEvent = new SmiteOperationEvent<T>(operation, user);
        RaiseLocalEvent(target, ref operationEvent);
    }
}
