namespace Content.Shared.Administration.Smites;

/// <summary>
/// Carries a typed smite operation and the admin responsible for it.
/// </summary>
[ByRefEvent]
public readonly record struct SmiteOperationEvent<T>(T Operation, EntityUid User) where T : SmiteOperationBase<T>;
