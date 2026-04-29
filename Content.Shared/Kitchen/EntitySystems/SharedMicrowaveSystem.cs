using Content.Shared.Item;
using Content.Shared.Kitchen.Components;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Kitchen.EntitySystems;

public abstract class SharedMicrowaveSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedPowerReceiverSystem Power = default!;
    [Dependency] private readonly SharedPowerStateSystem PowerState = default!;
    [Dependency] protected readonly KitchenDeviceSystem _kitchenDevice = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MicrowaveComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MicrowaveComponent, ContainerModifiedMessage>(OnContentUpdate);
        SubscribeLocalEvent<MicrowaveComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<MicrowaveComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<MicrowaveComponent, MicrowaveEjectMessage>(OnEjectMessage);
        SubscribeLocalEvent<MicrowaveComponent, MicrowaveEjectSolidIndexedMessage>(OnEjectIndex);
        SubscribeLocalEvent<MicrowaveComponent, MicrowaveSelectCookTimeMessage>(OnSelectTime);
    }

    private void OnInit(Entity<MicrowaveComponent> ent, ref ComponentInit args)
    {
        // this really does have to be in ComponentInit
        ent.Comp.Storage = _kitchenDevice.EnsureContainer(ent, ent.Comp.ContainerId);
    }

    private void OnContentUpdate(Entity<MicrowaveComponent> ent, ref ContainerModifiedMessage args) // For some reason ContainerModifiedMessage just can't be used at all with Entity<T>. TODO: replace with Entity<T> syntax once that's possible
    {
        if (ent.Comp.Storage != args.Container)
            return;

        UpdateUserInterfaceState(ent, ent.Comp);
    }

    private void OnInsertAttempt(Entity<MicrowaveComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        if (!_kitchenDevice.CanInsertItem(ent, args.EntityUid, ent.Comp.Storage, ent.Comp.Capacity, null, ent.Comp.Broken, ent.Comp.IsOperating))
        {
            args.Cancel();
            return;
        }

        if (!TryComp<ItemComponent>(args.EntityUid, out _) ||
            !_kitchenDevice.ItemFitsInDevice(ent.Comp.Storage, ent.Comp.Capacity, args.EntityUid, ent.Comp.MaxItemSize))
        {
            args.Cancel();
        }
    }

    protected virtual void OnPowerChanged(Entity<MicrowaveComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            StopCooking(ent);
        }
        UpdateUserInterfaceState(ent, ent.Comp);
    }

    #region UI Messages

    private void OnEjectMessage(Entity<MicrowaveComponent> ent, ref MicrowaveEjectMessage args)
    {
        if (ent.Comp.IsOperating)
            return;

        if (!KitchenDeviceSystem.HasContents(ent.Comp.Storage))
            return;

        _audio.PlayPredicted(ent.Comp.ClickSound, ent.Owner, args.Actor);
        _kitchenDevice.EjectAll(ent.Comp.Storage);
        UpdateUserInterfaceState(ent, ent.Comp);
    }

    private void OnEjectIndex(Entity<MicrowaveComponent> ent, ref MicrowaveEjectSolidIndexedMessage args)
    {
        if (ent.Comp.IsOperating)
            return;

        var entity = GetEntity(args.EntityID);
        if (!ent.Comp.Storage.Contains(entity))
            return;

        _audio.PlayPredicted(ent.Comp.ClickSound, ent.Owner, args.Actor);
        _container.Remove(entity, ent.Comp.Storage);
        UpdateUserInterfaceState(ent, ent.Comp);
    }

    private void OnSelectTime(Entity<MicrowaveComponent> ent, ref MicrowaveSelectCookTimeMessage args)
    {
        if (!KitchenDeviceSystem.HasContents(ent.Comp.Storage) || ent.Comp.IsOperating || !Power.IsPowered(ent.Owner))
            return;

        if (!ValidateCookTime(args.NewCookTime, ent.Comp.MaxCookTime))
            return;

        ent.Comp.CurrentCookTimeButtonIndex = args.ButtonIndex;
        ent.Comp.CurrentCookTimerTime = args.NewCookTime;
        ent.Comp.CurrentCookTimeEnd = TimeSpan.Zero;
        _audio.PlayPredicted(ent.Comp.ClickSound, ent, args.Actor);
        UpdateUserInterfaceState(ent, ent.Comp);
    }

    #endregion

    #region Active State Tracking

    protected virtual void OnCookStart(EntityUid uid, MicrowaveComponent component)
    {
        PowerState.TrySetWorkingState(uid, true);
    }

    protected virtual void OnCookStop(EntityUid uid, MicrowaveComponent component)
    {
        PowerState.TrySetWorkingState(uid, false);
    }

    #endregion

    #region Timer Logic

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveMicrowaveComponent, MicrowaveComponent>();
        while (query.MoveNext(out var uid, out _, out var microwave))
        {
            if (!microwave.EndTime.HasValue || Timing.CurTime < microwave.EndTime.Value)
                continue;

            var remainingHeatTime = Math.Max(frameTime - (float)(Timing.CurTime - microwave.EndTime.Value).TotalSeconds, 0);
            OnCookingComplete(uid, microwave, remainingHeatTime);
        }
    }

    protected virtual void OnCookingComplete(EntityUid uid, MicrowaveComponent microwave, float remainingHeatTime)
    {
        microwave.EndTime = null;
        microwave.TimeRemaining = 0;
        UpdateUserInterfaceState(uid, microwave);
        StopCooking((uid, microwave));
    }

    #endregion

    #region Helper Methods

    protected abstract void UpdateUserInterfaceState(EntityUid uid, MicrowaveComponent component);

    protected abstract void StopCooking(Entity<MicrowaveComponent> ent);

    private static bool ValidateCookTime(uint cookTime, uint maxTime, uint step = 5)
    {
        return cookTime % step == 0 && cookTime <= maxTime;
    }

    #endregion
}
