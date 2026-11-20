using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Mech.Components;
using Content.Shared.Atmos;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Systems;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Vehicle.Components;
using Content.Shared.Wires;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Mech.Systems;

/// <inheritdoc/>
public sealed partial class ServerMechSystem : MechSystem
{
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private SharedToolSystem _toolSystem = default!;

    private static readonly ProtoId<ToolQualityPrototype> PryingQuality = "Prying";

    [SubscribeLocalEvent]
    private void OnInteractUsing(EntityUid uid, MechComponent component, InteractUsingEvent args)
    {
        if (TryComp<WiresPanelComponent>(uid, out var panel) && !panel.Open)
            return;

        if (component.BatterySlot.ContainedEntity == null && TryComp<BatteryComponent>(args.Used, out var battery))
        {
            InsertBattery((uid, component), (args.Used, battery));
            return;
        }

        if (!_toolSystem.HasQuality(args.Used, PryingQuality) || component.BatterySlot.ContainedEntity == null)
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.BatteryRemovalDelay,
            new RemoveBatteryEvent(), uid, target: uid, used: args.Used)
        {
            BreakOnMove = true
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    [SubscribeLocalEvent]
    private void OnInsertBattery(EntityUid uid, MechComponent component, EntInsertedIntoContainerMessage args)
    {
        if (args.Container != component.BatterySlot || !TryComp<BatteryComponent>(args.Entity, out var battery))
            return;

        component.Energy = _battery.GetCharge((args.Entity, battery));
        component.MaxEnergy = battery.MaxCharge;

        Dirty(uid, component);
        Vehicle.RefreshCanRun(uid);
    }

    [SubscribeLocalEvent]
    private void OnRemoveBattery(EntityUid uid, MechComponent component, RemoveBatteryEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        RemoveBattery((uid, component));

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnMapInit(EntityUid uid, MechComponent component, MapInitEvent args)
    {
        // TODO: this should just be damage and battery
        component.Integrity = component.MaxIntegrity;
        component.Energy = component.MaxEnergy;

        Vehicle.RefreshCanRun(uid);
        Dirty(uid, component);
    }

    [SubscribeLocalEvent]
    private void OnRemoveEquipmentMessage(EntityUid uid, MechComponent component, MechEquipmentRemoveMessage args)
    {
        var equip = GetEntity(args.Equipment);

        if (!Exists(equip) || Deleted(equip))
            return;

        if (!component.EquipmentContainer.ContainedEntities.Contains(equip))
            return;

        RemoveEquipment((uid, component), equip);
    }

    [SubscribeLocalEvent]
    private void OnToolUseAttempt(Entity<VehicleOperatorComponent> ent, ref ToolUserAttemptUseEvent args)
    {
        if (ent.Comp.Vehicle is { } vehicle && args.Target == vehicle)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void RelayGrabberUiMessage(Entity<MechComponent> ent, ref MechGrabberEjectMessage args)
    {
        ReceiveEquipmentUiMessages(ent, args);
    }

    [SubscribeLocalEvent]
    private void RelaySoundboardUiMessage(Entity<MechComponent> ent, ref MechSoundboardPlayMessage args)
    {
        ReceiveEquipmentUiMessages(ent, args);
    }

    public override void UpdateUserInterface(Entity<MechComponent> ent)
    {
        base.UpdateUserInterface(ent);

        var ev = new MechEquipmentUiStateReadyEvent();
        foreach (var equip in ent.Comp.EquipmentContainer.ContainedEntities)
        {
            RaiseLocalEvent(equip, ev);
        }

        var state = new MechBoundUiState
        {
            EquipmentStates = ev.States
        };
        _ui.SetUiState(ent.Owner, MechUiKey.Key, state);
    }

    public override void BreakMech(Entity<MechComponent> ent)
    {
        base.BreakMech(ent);

        _ui.CloseUi(ent.Owner, MechUiKey.Key);
        Vehicle.RefreshCanRun(ent.Owner);
    }

    public override bool TryChangeEnergy(Entity<MechComponent> ent, FixedPoint2 delta)
    {
        if (!base.TryChangeEnergy(ent, delta))
            return false;

        var battery = ent.Comp.BatterySlot.ContainedEntity;
        if (battery == null)
            return false;

        if (!TryComp<BatteryComponent>(battery, out var batteryComp))
            return false;

        _battery.SetCharge((battery.Value, batteryComp), _battery.GetCharge((battery.Value, batteryComp)) + delta.Float());
        // TODO: Power cells are predicted now, so no need to duplicate the charge level
        var charge = _battery.GetCharge((battery.Value, batteryComp));
        if (charge != ent.Comp.Energy) //if there's a discrepency, we have to resync them
        {
            Log.Debug($"Battery charge was not equal to mech charge. Battery {charge}. Mech {ent.Comp.Energy}");
            ent.Comp.Energy = charge;
            Dirty(ent);
        }
        Vehicle.RefreshCanRun(ent.Owner);
        return true;
    }

    public void InsertBattery(Entity<MechComponent> mech, Entity<BatteryComponent> toInsert)
    {
        _container.Insert(toInsert.Owner, mech.Comp.BatterySlot);
        mech.Comp.Energy = _battery.GetCharge((toInsert.Owner, toInsert.Comp));
        mech.Comp.MaxEnergy = toInsert.Comp.MaxCharge;

        Vehicle.RefreshCanRun(mech.Owner);

        Dirty(mech);
        UpdateUserInterface(mech);
    }

    public void RemoveBattery(Entity<MechComponent> ent)
    {
        _container.EmptyContainer(ent.Comp.BatterySlot);
        ent.Comp.Energy = 0;
        ent.Comp.MaxEnergy = 0;

        Vehicle.RefreshCanRun(ent.Owner);

        Dirty(ent);
        UpdateUserInterface(ent);
    }

    #region Atmos Handling
    [SubscribeLocalEvent]
    private void OnInhale(Entity<VehicleOperatorComponent> ent, ref InhaleLocationEvent args)
    {
        if (ent.Comp.Vehicle is not { } vehicle ||
            !TryComp<MechComponent>(vehicle, out var mech) ||
            !TryComp<MechAirComponent>(vehicle, out var mechAir))
        {
            return;
        }

        if (mech.Airtight)
            args.Gas = mechAir.Air;
    }

    [SubscribeLocalEvent]
    private void OnExhale(Entity<VehicleOperatorComponent> ent, ref ExhaleLocationEvent args)
    {
        if (ent.Comp.Vehicle is not { } vehicle ||
            !TryComp<MechComponent>(vehicle, out var mech) ||
            !TryComp<MechAirComponent>(vehicle, out var mechAir))
        {
            return;
        }

        if (mech.Airtight)
            args.Gas = mechAir.Air;
    }

    [SubscribeLocalEvent]
    private void OnExpose(Entity<VehicleOperatorComponent> ent, ref AtmosExposedGetAirEvent args)
    {
        if (args.Handled || ent.Comp.Vehicle is not { } vehicle)
            return;

        if (!TryComp(vehicle, out MechComponent? mech))
            return;

        if (mech.Airtight && TryComp(vehicle, out MechAirComponent? air))
        {
            args.Handled = true;
            args.Gas = air.Air;
            return;
        }

        args.Gas = _atmosphere.GetContainingMixture(vehicle, excite: args.Excite);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnGetFilterAir(Entity<MechAirComponent> ent, ref GetFilterAirEvent args)
    {
        if (args.Air != null)
            return;

        // only airtight mechs get internal air
        if (!TryComp<MechComponent>(ent, out var mech) || !mech.Airtight)
            return;

        args.Air = ent.Comp.Air;
    }
    #endregion
}
