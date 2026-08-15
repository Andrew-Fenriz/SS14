namespace Content.Shared.Administration.Smites;

/// <summary>
/// A single operation performed by a declarative admin smite.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class SmiteOperation
{
    /// <summary>
    /// Raises the correctly typed event for this operation.
    /// </summary>
    public abstract void RaiseEvent(EntityUid target, EntityUid user, ISmiteOperationRaiser raiser);
}

/// <summary>
/// Preserves the concrete operation type when raising its event.
/// </summary>
public abstract partial class SmiteOperationBase<T> : SmiteOperation where T : SmiteOperationBase<T>
{
    public override void RaiseEvent(EntityUid target, EntityUid user, ISmiteOperationRaiser raiser)
    {
        if (this is T operation)
            raiser.RaiseOperationEvent(target, user, operation);
    }
}

/// <summary>
/// Raises smite operation events without losing their concrete type.
/// </summary>
public interface ISmiteOperationRaiser
{
    void RaiseOperationEvent<T>(EntityUid target, EntityUid user, T operation) where T : SmiteOperationBase<T>;
}

/// <summary>
/// Carries a typed smite operation and the admin responsible for it.
/// </summary>
[ByRefEvent]
public readonly record struct SmiteOperationEvent<T>(T Operation, EntityUid User) where T : SmiteOperationBase<T>;
