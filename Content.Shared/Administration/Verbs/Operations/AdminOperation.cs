namespace Content.Shared.Administration.Verbs.Operations;

/// <summary>
/// A single operation performed by a declarative admin verb.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class AdminOperation
{
    /// <summary>
    /// Raises the correctly typed event for this operation.
    /// </summary>
    public abstract void RaiseEvent(EntityUid target, EntityUid user, IAdminOperationRaiser raiser);
}

/// <summary>
/// Preserves the concrete operation type when raising its event.
/// </summary>
public abstract partial class AdminOperationBase<T> : AdminOperation where T : AdminOperationBase<T>
{
    public override void RaiseEvent(EntityUid target, EntityUid user, IAdminOperationRaiser raiser)
    {
        if (this is T operation)
            raiser.RaiseOperationEvent(target, user, operation);
    }
}

/// <summary>
/// Raises admin operation events without losing their concrete type.
/// </summary>
public interface IAdminOperationRaiser
{
    void RaiseOperationEvent<T>(EntityUid target, EntityUid user, T operation) where T : AdminOperationBase<T>;
}

/// <summary>
/// Carries a typed admin operation and the admin responsible for it.
/// </summary>
[ByRefEvent]
public readonly record struct AdminOperationEvent<T>(T Operation, EntityUid User) where T : AdminOperationBase<T>;
