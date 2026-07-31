using System.Numerics;

namespace Content.Client.Buckle;

/// <summary>
/// Tracks the visual offset applied to an entity by a strap.
/// </summary>
/// <remarks>
/// This is client-only runtime state. It is not intended for use in prototypes.
/// </remarks>
[RegisterComponent]
public sealed partial class StrapVisualsOffsetComponent : Component
{
    /// <summary>
    /// The strap supplying the current visuals.
    /// </summary>
    public EntityUid Strap;

    /// <summary>
    /// The apparent direction used to select the current visuals.
    /// </summary>
    public Direction? Direction;

    /// <summary>
    /// The portion of the sprite offset applied by the strap visuals system.
    /// </summary>
    public Vector2 AppliedOffset;
}
