using Content.Shared.Item;
using Content.Shared.Kitchen.Components;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Kitchen.EntitySystems;

public abstract class SharedMicrowaveSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedPowerReceiverSystem Power = default!;
    [Dependency] protected readonly KitchenDeviceSystem _kitchenDevice = default!;

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

        SubscribeLocalEvent<ActiveKitchenDeviceComponent, ComponentStartup>(OnCookStart);
        SubscribeLocalEvent<ActiveKitchenDeviceComponent, ComponentShutdown>(OnCookStop);
    }

    private void OnInit(Entity<MicrowaveComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Storage = _kitchenDevice.EnsureContainer(ent, ent.Comp.ContainerId);
    }

    private void OnContentUpdate(Entity<MicrowaveComponent> ent, ref ContainerModifiedMessage args)
    {
        if (ent.Comp.Storage != args.Container)
            return;

        UpdateUserInterfaceState(ent, ent.Comp);
    }

    protected virtual void OnInsertAttempt(Entity<MicrowaveComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        // Check basic insertion requirements (not broken, not active, has capacity)
        if (!_kitchenDevice.CanInsertItem(ent, args.EntityUid, ent.Comp.Storage, ent.Comp.Capacity, null, ent.Comp.Broken))
        {
            args.Cancel();
            return;
        }

        // Check item size constraint
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
        if (HasComp<ActiveKitchenDeviceComponent>(ent))
            return;

        if (_kitchenDevice.HandleEjectAll(ent, ent.Comp.Storage, ent.Comp.ClickSound, args.Actor))
            UpdateUserInterfaceState(ent, ent.Comp);
    }

    private void OnEjectIndex(Entity<MicrowaveComponent> ent, ref MicrowaveEjectSolidIndexedMessage args)
    {
        if (HasComp<ActiveKitchenDeviceComponent>(ent))
            return;

        var entity = GetEntity(args.EntityID);
        if (_kitchenDevice.HandleEjectItem(ent, entity, ent.Comp.Storage, ent.Comp.ClickSound, args.Actor))
            UpdateUserInterfaceState(ent, ent.Comp);
    }

    private void OnSelectTime(Entity<MicrowaveComponent> ent, ref MicrowaveSelectCookTimeMessage args)
    {
        if (!KitchenDeviceSystem.HasContents(ent.Comp.Storage) || HasComp<ActiveKitchenDeviceComponent>(ent) || !Power.IsPowered(ent.Owner))
            return;

        // Validation to prevent trollage
        if (!KitchenDeviceSystem.ValidateCookTime(args.NewCookTime, ent.Comp.MaxCookTime))
            return;

        ent.Comp.CurrentCookTimeButtonIndex = args.ButtonIndex;
        ent.Comp.CurrentCookTimerTime = args.NewCookTime;
        ent.Comp.CurrentCookTimeEnd = TimeSpan.Zero;
        _kitchenDevice.PlayClickSound(ent, ent.Comp.ClickSound, args.Actor);
        UpdateUserInterfaceState(ent, ent.Comp);
    }

    #endregion

    #region Active State Tracking

    private void OnCookStart(Entity<ActiveKitchenDeviceComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<MicrowaveComponent>(ent, out var microwaveComponent))
            return;

        OnCookStart(ent.Owner, microwaveComponent);
    }

    private void OnCookStop(Entity<ActiveKitchenDeviceComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<MicrowaveComponent>(ent, out var microwaveComponent))
            return;

        OnCookStop(ent.Owner, microwaveComponent);
    }

    protected virtual void OnCookStart(EntityUid uid, MicrowaveComponent component)
    {
        _kitchenDevice.SetWorkingState(uid, true);
    }

    protected virtual void OnCookStop(EntityUid uid, MicrowaveComponent component)
    {
        _kitchenDevice.SetWorkingState(uid, false);
    }

    #endregion

    #region Timer Logic

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveKitchenDeviceComponent, MicrowaveComponent>();
        while (query.MoveNext(out var uid, out var active, out var microwave))
        {
            if (!_kitchenDevice.ProcessTimer(uid, frameTime, out var remainingHeatTime))
                continue;

            // Microwave has finished cooking
            OnCookingComplete(uid, active, microwave, remainingHeatTime);
        }
    }

    protected virtual void OnCookingComplete(EntityUid uid, ActiveKitchenDeviceComponent active, MicrowaveComponent microwave, float remainingHeatTime)
    {
        microwave.CurrentCookTimeEnd = TimeSpan.Zero;
        UpdateUserInterfaceState(uid, microwave);
        StopCooking((uid, microwave));
    }

    #endregion

    #region Helper Methods

    protected abstract void UpdateUserInterfaceState(EntityUid uid, MicrowaveComponent component);

    protected abstract void StopCooking(Entity<MicrowaveComponent> ent);

    #endregion
}
