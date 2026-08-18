using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Smites;

/// <summary>
/// Adds an action to the target if it does not already have one from the same prototype.
/// </summary>
public sealed partial class AddActionSmite : SmiteOperationBase<AddActionSmite>
{
    [DataField(required: true)]
    public EntProtoId Action { get; private set; }
}

/// <summary>
/// Adds a mind role to the target's mind if it does not already have one from the same prototype.
/// </summary>
public sealed partial class AddMindRoleSmite : SmiteOperationBase<AddMindRoleSmite>
{
    [DataField(required: true)]
    public EntProtoId Role { get; private set; }
}

/// <summary>
/// Adds or replaces bound user interfaces on the target.
/// </summary>
public sealed partial class AddUserInterfacesSmite : SmiteOperationBase<AddUserInterfacesSmite>
{
    [DataField(required: true)]
    public Dictionary<Enum, InterfaceData> Interfaces { get; private set; } = new();
}

/// <summary>
/// Disconnects the smite target from their current session.
/// </summary>
public sealed partial class GhostKickSmite : SmiteOperationBase<GhostKickSmite>;

/// <summary>
/// Configures the target to be bound to silicon laws.
/// </summary>
public sealed partial class SiliconLawBoundSmite : SmiteOperationBase<SiliconLawBoundSmite>;
