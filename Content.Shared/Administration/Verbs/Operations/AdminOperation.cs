namespace Content.Shared.Administration.Verbs.Operations;

/// <summary>
/// An operation executed by a prototype-backed admin verb.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class AdminOperation
{
    public abstract void RaiseEvent(EntityUid target, EntityUid user, IAdminOperationRaiser raiser);
}

/// <summary>
/// Keeps the concrete operation type when dispatching it to a handler.
/// </summary>
public abstract partial class AdminOperationBase<T> : AdminOperation where T : AdminOperationBase<T>
{
    public override void RaiseEvent(EntityUid target, EntityUid user, IAdminOperationRaiser raiser)
    {
        raiser.RaiseOperationEvent(target, user, (T) this);
    }
}

/// <summary>
/// Dispatches typed admin operations to their target.
/// </summary>
public interface IAdminOperationRaiser
{
    void RaiseOperationEvent<T>(EntityUid target, EntityUid user, T operation) where T : AdminOperationBase<T>;
}

/// <summary>
/// Carries a typed operation and the admin that invoked it.
/// </summary>
[ByRefEvent]
public readonly record struct AdminOperationEvent<T>(T Operation, EntityUid User) where T : AdminOperationBase<T>;
