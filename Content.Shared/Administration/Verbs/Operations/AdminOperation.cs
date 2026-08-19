namespace Content.Shared.Administration.Verbs.Operations;

/// <summary>
/// Base type for an operation in a declarative admin verb's execution sequence.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class AdminOperation
{
    /// <summary>
    /// Dispatches this operation to its typed handler on the target.
    /// </summary>
    /// <param name="target">Entity the operation acts on.</param>
    /// <param name="user">Entity that invoked the parent admin verb.</param>
    /// <param name="raiser">Dispatcher used to raise the concrete operation event.</param>
    public abstract void RaiseEvent(EntityUid target, EntityUid user, IAdminOperationRaiser raiser);
}

/// <summary>
/// Self-typed base for admin operations dispatched as strongly typed local events.
/// </summary>
/// <typeparam name="T">Concrete operation type.</typeparam>
public abstract partial class AdminOperationBase<T> : AdminOperation where T : AdminOperationBase<T>
{
    /// <inheritdoc />
    public override void RaiseEvent(EntityUid target, EntityUid user, IAdminOperationRaiser raiser)
    {
        if (this is T operation)
            raiser.RaiseOperationEvent(target, user, operation);
    }
}

/// <summary>
/// Dispatches concrete admin operations as typed local events.
/// </summary>
public interface IAdminOperationRaiser
{
    /// <summary>
    /// Raises an operation event on the target.
    /// </summary>
    /// <typeparam name="T">Concrete operation type.</typeparam>
    /// <param name="target">Entity the event is raised on.</param>
    /// <param name="user">Entity that invoked the parent admin verb.</param>
    /// <param name="operation">Operation configuration to execute.</param>
    void RaiseOperationEvent<T>(EntityUid target, EntityUid user, T operation) where T : AdminOperationBase<T>;
}

/// <summary>
/// Raised on a target entity to execute a concrete admin operation.
/// </summary>
/// <typeparam name="T">Concrete operation type.</typeparam>
/// <param name="Operation">Operation configuration to execute.</param>
/// <param name="User">Entity that invoked the parent admin verb.</param>
[ByRefEvent]
public readonly record struct AdminOperationEvent<T>(T Operation, EntityUid User) where T : AdminOperationBase<T>;
