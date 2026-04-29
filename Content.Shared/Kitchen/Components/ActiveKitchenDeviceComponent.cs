using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Kitchen.Components;

/// <summary>
/// Attached to a kitchen device that is currently in the process of working.
/// Base component for microwave, reagent grinder, and other kitchen devices.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class ActiveKitchenDeviceComponent : Component
{
    /// <summary>
    /// Time remaining for the operation to complete.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float TimeRemaining;

    /// <summary>
    /// Total time for the operation (for UI display and progress calculation).
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float TotalTime;

    /// <summary>
    /// The time multiplier affecting operation speed.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float TimeMultiplier = 1.0f;

    /// <summary>
    /// Time when the device will finish (for server-side tracking).
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan? EndTime;

    /// <summary>
    /// The current operating mode of the device (e.g., grind, juice, cook).
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string? Mode;

    /// <summary>
    /// Recipe ID being processed (for devices that use recipes).
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string? RecipeId;

    /// <summary>
    /// Number of portions being made.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public int PortionCount;

    /// <summary>
    /// Time of next malfunction check (for devices that can malfunction).
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan MalfunctionTime = TimeSpan.Zero;

    /// <summary>
    /// Auto mode setting for the device (e.g., auto-grind, auto-juice).
    /// When enabled, the device will automatically start when items are inserted.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public GrinderAutoMode AutoMode = GrinderAutoMode.Off;
}
