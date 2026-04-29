using Robust.Shared.Serialization;

namespace Content.Shared.Kitchen;

[Serializable, NetSerializable]
public sealed class MicrowaveStartCookMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class MicrowaveEjectMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class MicrowaveEjectSolidIndexedMessage(NetEntity entityId) : BoundUserInterfaceMessage
{
    public NetEntity EntityID = entityId;
}

[Serializable, NetSerializable]
public sealed class MicrowaveSelectCookTimeMessage(int buttonIndex, uint inputTime) : BoundUserInterfaceMessage
{
    public int ButtonIndex = buttonIndex;
    public uint NewCookTime = inputTime;
}

[NetSerializable, Serializable]
public sealed class MicrowaveUpdateUserInterfaceState(
    NetEntity[] containedSolids,
    bool isMicrowaveBusy,
    int activeButtonIndex,
    uint currentCookTime,
    TimeSpan currentCookTimeEnd)
    : BoundUserInterfaceState
{
    public NetEntity[] ContainedSolids = containedSolids;
    public bool IsMicrowaveBusy = isMicrowaveBusy;
    public int ActiveButtonIndex = activeButtonIndex;
    public uint CurrentCookTime = currentCookTime;
    public TimeSpan CurrentCookTimeEnd = currentCookTimeEnd;
}

[Serializable, NetSerializable]
public enum MicrowaveVisualState
{
    Idle,
    Cooking,
    Broken,
    Bloody
}

[NetSerializable, Serializable]
public enum MicrowaveUiKey
{
    Key
}
