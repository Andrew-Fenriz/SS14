namespace Content.Shared.Administration.Smites;

/// <summary>
/// Makes the smite target bounce with a random impulse.
/// </summary>
public sealed partial class PinballSmite : SmiteOperationBase<PinballSmite>;

/// <summary>
/// Swaps the smite target's base walking and sprinting speeds.
/// </summary>
public sealed partial class SwapMovementSpeedsSmite : SmiteOperationBase<SwapMovementSpeedsSmite>;

/// <summary>
/// Launches the smite target with non-solid fixtures.
/// </summary>
public sealed partial class YeetSmite : SmiteOperationBase<YeetSmite>;
