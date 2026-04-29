using Content.Shared.Kitchen.Components;

namespace Content.Shared.Kitchen.EntitySystems;

public partial class KitchenDeviceSystem
{
    public void StartTimer(EntityUid uid, float duration, float multiplier = 1.0f, string? mode = null)
    {
        var activeComp = EnsureComp<ActiveKitchenDeviceComponent>(uid);
        var scaledDuration = duration * multiplier;

        activeComp.TimeRemaining = scaledDuration;
        activeComp.TotalTime = duration;
        activeComp.TimeMultiplier = multiplier;
        activeComp.EndTime = _timing.CurTime + TimeSpan.FromSeconds(scaledDuration);
        activeComp.Mode = mode;

        Dirty(uid, activeComp);
    }

    public void StopTimer(EntityUid uid)
    {
        if (!HasComp<ActiveKitchenDeviceComponent>(uid))
            return;

        RemCompDeferred<ActiveKitchenDeviceComponent>(uid);
    }

    public bool IsActive(EntityUid uid)
    {
        return HasComp<ActiveKitchenDeviceComponent>(uid);
    }

    public bool ProcessTimer(EntityUid uid, float frameTime, out float remainingHeatTime)
    {
        remainingHeatTime = 0;

        if (!TryComp<ActiveKitchenDeviceComponent>(uid, out var active))
            return false;

        active.TimeRemaining -= frameTime;

        if (active.TimeRemaining > 0)
            return false;

        // Operation complete
        remainingHeatTime = Math.Max(frameTime + active.TimeRemaining, 0);
        return true;
    }

    public void SetMalfunctionTime(EntityUid uid, float intervalSeconds)
    {
        if (!TryComp<ActiveKitchenDeviceComponent>(uid, out var activeComp))
            return;

        activeComp.MalfunctionTime = _timing.CurTime + TimeSpan.FromSeconds(intervalSeconds);
        Dirty(uid, activeComp);
    }

    /// <summary>
    /// Validates that the cook time is within limits and matches the step interval.
    /// </summary>
    public static bool ValidateCookTime(uint cookTime, uint maxTime, uint step = 5)
    {
        return cookTime % step == 0 && cookTime <= maxTime;
    }
}
