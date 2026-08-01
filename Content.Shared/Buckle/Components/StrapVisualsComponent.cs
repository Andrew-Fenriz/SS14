namespace Content.Shared.Buckle.Components;

/// <summary>
/// Defines direction-dependent visuals for entities buckled to a <see cref="StrapComponent"/>.
/// </summary>
/// <remarks>
/// These visuals must not affect the transform or physics of the buckled entity.
/// Directions are relative to the apparent direction of the strap on screen.
/// </remarks>
[RegisterComponent]
[Access(typeof(SharedBuckleSystem))]
public sealed partial class StrapVisualsComponent : Component
{
    /// <summary>
    /// Visual data to apply to buckled entities for each cardinal direction.
    /// Missing directions apply no visual changes.
    /// </summary>
    [DataField]
    public Dictionary<Direction, StrapDirectionVisuals> Directions = new();
}

/// <summary>
/// Visual properties applied to an entity buckled to a strap in a particular direction.
/// </summary>
[DataDefinition]
public sealed partial class StrapDirectionVisuals
{
    /// <summary>
    /// An offset, in pixels, added to the buckled entity's sprite without changing its transform.
    /// </summary>
    [DataField]
    public Vector2i PixelOffset;
}
