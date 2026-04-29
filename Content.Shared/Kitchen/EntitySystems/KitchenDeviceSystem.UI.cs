using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Shared.Kitchen.EntitySystems;

public partial class KitchenDeviceSystem
{
    /// <summary>
    /// Sets the working state of the device (for power consumption).
    /// </summary>
    public void SetWorkingState(EntityUid uid, bool working)
    {
        _powerState.TrySetWorkingState(uid, working);
    }

    /// <summary>
    /// Checks if the device can be used for cooking/grinding.
    /// </summary>
    public bool CanDeviceBeUsed(EntityUid uid, Container container, bool isBroken, bool requireContents = true)
    {
        if (isBroken)
            return false;

        if (IsActive(uid))
            return false;

        if (!_power.IsPowered(uid))
            return false;

        return !requireContents || HasContents(container);
    }

    /// <summary>
    /// Ejects all items from the container and plays the click sound.
    /// </summary>
    public bool HandleEjectAll(EntityUid uid, Container container, SoundSpecifier clickSound, EntityUid? actor = null)
    {
        if (!HasContents(container))
            return false;

        PlayClickSound(uid, clickSound, actor);
        EjectAll(container);
        return true;
    }

    /// <summary>
    /// Ejects a specific item from the container and plays the click sound.
    /// </summary>
    public bool HandleEjectItem(EntityUid uid, EntityUid item, Container container, SoundSpecifier clickSound, EntityUid? actor = null)
    {
        if (!container.Contains(item))
            return false;

        PlayClickSound(uid, clickSound, actor);
        EjectEntity(item, container);
        return true;
    }
}
