using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Systems;
using Content.Shared.Vehicle.Components;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Mech.Systems;

/// <summary>
/// Handles all of the interactions, UI handling, and items shennanigans for <see cref="MechComponent"/>
/// </summary>
public abstract partial class MechSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] protected VehicleSystem Vehicle = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;

    [SubscribeLocalEvent]
    private void OnMechCanMoveEvent(Entity<MechComponent> ent, ref VehicleCanRunEvent args)
    {
        if (ent.Comp.Broken || ent.Comp.Integrity <= 0 || ent.Comp.Energy <= 0)
            args.CanRun = false;
    }

    [SubscribeLocalEvent]
    private void OnDamageChanged(EntityUid uid, MechComponent component, DamageChangedEvent args)
    {
        var integrity = component.MaxIntegrity - _damageable.GetTotalDamage((uid, args.Damageable));
        SetIntegrity((uid, component), integrity);
    }

    [SubscribeLocalEvent]
    private void OnToggleEquipmentAction(EntityUid uid, MechComponent component, MechToggleEquipmentEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;
        CycleEquipment((uid, component));
    }

    [SubscribeLocalEvent]
    private void OnEjectPilotEvent(EntityUid uid, MechComponent component, MechEjectPilotEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;
        Vehicle.TryExit(uid);
    }

    [SubscribeLocalEvent]
    private void OnOpenUi(EntityUid uid, MechComponent component, MechOpenUiEvent args)
    {
        args.Handled = true;
        ToggleMechUi((uid, component));
    }

    [SubscribeLocalEvent]
    private void OnAlternativeVerb(EntityUid uid, MechComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess ||
            !args.CanInteract ||
            component.Broken ||
            Vehicle.HasOperator(uid))
            return;

        var openUiVerb = new AlternativeVerb
        {
            Act = () => ToggleMechUi((uid, component), args.User),
            Text = Loc.GetString("mech-ui-open-verb")
        };

        args.Verbs.Add(openUiVerb);
    }

    [SubscribeLocalEvent]
    private void RelayInteractionEvent(EntityUid uid, MechComponent component, UserActivateInWorldEvent args)
    {
        if (!Vehicle.HasOperator(uid))
            return;

        // TODO why is this being blocked?
        if (!_timing.IsFirstTimePredicted)
            return;

        if (component.CurrentSelectedEquipment != null)
        {
            RaiseLocalEvent(component.CurrentSelectedEquipment.Value, args);
        }
    }

    [SubscribeLocalEvent]
    private void OnStartup(EntityUid uid, MechComponent component, ComponentStartup args)
    {
        component.EquipmentContainer = _container.EnsureContainer<Container>(uid, component.EquipmentContainerId);
        component.BatterySlot = _container.EnsureContainer<ContainerSlot>(uid, component.BatterySlotId);
        UpdateAppearance((uid, component));
    }

    [SubscribeLocalEvent]
    private void OnDestruction(EntityUid uid, MechComponent component, DestructionEventArgs args)
    {
        BreakMech((uid, component));
    }

    [SubscribeLocalEvent]
    private void OnEntityStorageDump(Entity<MechComponent> entity, ref EntityStorageIntoContainerAttemptEvent args)
    {
        // There's no reason we should dump into /any/ of the mech's containers.
        args.Cancelled = true;
    }

    private void SetupUser(Entity<MechComponent> mech, EntityUid pilot)
    {
        // Warning: this bypasses most normal interaction blocking components on the user, like drone laws and the like.
        var irelay = EnsureComp<InteractionRelayComponent>(pilot);
        _interaction.SetRelay(pilot, mech, irelay);

        if (_net.IsClient)
            return;

        _actions.AddAction(pilot, ref mech.Comp.MechCycleActionEntity, mech.Comp.MechCycleAction, mech);
        _actions.AddAction(pilot, ref mech.Comp.MechUiActionEntity, mech.Comp.MechUiAction, mech);
        _actions.AddAction(pilot, ref mech.Comp.MechEjectActionEntity, mech.Comp.MechEjectAction, mech);
    }

    private void RemoveUser(Entity<MechComponent> mech, EntityUid pilot)
    {
        RemComp<InteractionRelayComponent>(pilot);
        _actions.RemoveProvidedActions(pilot, mech);
    }

    /// <summary>
    /// Destroys the mech, removing the user and ejecting anything contained.
    /// </summary>
    public virtual void BreakMech(Entity<MechComponent> ent)
    {
        Vehicle.TryExit(ent.Owner);
        var equipment = new List<EntityUid>(ent.Comp.EquipmentContainer.ContainedEntities);
        foreach (var equip in equipment)
        {
            RemoveEquipment(ent, equip, forced: true);
        }

        ent.Comp.Broken = true;
        UpdateAppearance(ent);
    }

    /// <summary>
    /// Cycles through the currently selected equipment.
    /// </summary>
    public void CycleEquipment(Entity<MechComponent> ent)
    {
        var allEquipment = ent.Comp.EquipmentContainer.ContainedEntities.ToList();

        var equipmentIndex = -1;
        if (ent.Comp.CurrentSelectedEquipment != null)
        {
            bool StartIndex(EntityUid u) => u == ent.Comp.CurrentSelectedEquipment;
            equipmentIndex = allEquipment.FindIndex(StartIndex);
        }

        equipmentIndex++;
        ent.Comp.CurrentSelectedEquipment = equipmentIndex >= allEquipment.Count
            ? null
            : allEquipment[equipmentIndex];

        var popupString = ent.Comp.CurrentSelectedEquipment != null
            ? Loc.GetString("mech-equipment-select-popup", ("item", ent.Comp.CurrentSelectedEquipment))
            : Loc.GetString("mech-equipment-select-none-popup");

        if (_net.IsServer)
            _popup.PopupEntity(popupString, ent);

        Dirty(ent);
    }

    /// <summary>
    /// Inserts an equipment item into the mech.
    /// </summary>
    public void InsertEquipment(Entity<MechComponent> mech, Entity<MechEquipmentComponent> toInsert)
    {
        if (mech.Comp.EquipmentContainer.ContainedEntities.Count >= mech.Comp.MaxEquipmentAmount)
            return;

        if (_whitelistSystem.IsWhitelistFail(mech.Comp.EquipmentWhitelist, toInsert))
            return;

        toInsert.Comp.EquipmentOwner = mech;
        _container.Insert(toInsert.Owner, mech.Comp.EquipmentContainer);
        var ev = new MechEquipmentInsertedEvent(mech.Owner);
        RaiseLocalEvent(toInsert, ref ev);
        UpdateUserInterface(mech);
    }

    /// <summary>
    /// Removes an equipment item from a mech.
    /// </summary>
    /// <param name="mech">The mech itself</param>
    /// <param name="toRemove">The equipment to remove</param>
    /// <param name="forced">Whether or not the removal can be cancelled, and if non-mech equipment should be ejected.</param>
    public void RemoveEquipment(Entity<MechComponent> mech, EntityUid toRemove, bool forced = false)
    {
        // When forced, we also want to handle the possibility that the "equipment" isn't actually equipment.
        // This /shouldn't/ be possible thanks to OnEntityStorageDump, but there's been quite a few regressions
        // with entities being hardlock stuck inside mechs.
        MechEquipmentComponent? equipmentComponent = null;
        if (!forced && !TryComp<MechEquipmentComponent>(toRemove, out equipmentComponent))
            return;

        if (!forced)
        {
            var attemptev = new AttemptRemoveMechEquipmentEvent();
            RaiseLocalEvent(toRemove, ref attemptev);
            if (attemptev.Cancelled)
                return;
        }

        var ev = new MechEquipmentRemovedEvent(mech.Owner);
        RaiseLocalEvent(toRemove, ref ev);

        if (mech.Comp.CurrentSelectedEquipment == toRemove)
            CycleEquipment(mech);

        if (forced)
        {
            if (TryComp<MechEquipmentComponent>(toRemove, out var eqComp))
                eqComp.EquipmentOwner = null;
        }
        else if (equipmentComponent != null)
        {
            equipmentComponent.EquipmentOwner = null;
        }

        _container.Remove(toRemove, mech.Comp.EquipmentContainer);
        UpdateUserInterface(mech);
    }

    /// <summary>
    /// Attempts to change the amount of energy in the mech.
    /// TODO: Power cells are predicted now, so no need to duplicate the charge level
    /// </summary>
    /// <param name="mech">The mech itself</param>
    /// <param name="delta">The change in energy</param>
    /// <returns>If the energy was successfully changed.</returns>
    public virtual bool TryChangeEnergy(Entity<MechComponent> mech, FixedPoint2 delta)
    {
        if (mech.Comp.Energy + delta < 0)
            return false;

        mech.Comp.Energy = FixedPoint2.Clamp(mech.Comp.Energy + delta, 0, mech.Comp.MaxEnergy);
        Dirty(mech);
        UpdateUserInterface(mech);
        return true;
    }

    /// <summary>
    /// Sets the integrity of the mech.
    /// </summary>
    /// <param name="mech">The mech itself</param>
    /// <param name="value">The value the integrity will be set at</param>
    public void SetIntegrity(Entity<MechComponent> mech, FixedPoint2 value)
    {
        mech.Comp.Integrity = FixedPoint2.Clamp(value, 0, mech.Comp.MaxIntegrity);

        if (mech.Comp.Integrity <= 0)
        {
            BreakMech(mech);
        }
        else if (mech.Comp.Broken)
        {
            mech.Comp.Broken = false;
            UpdateAppearance(mech);
        }

        Dirty(mech);
        UpdateUserInterface(mech);
    }

    /// <summary>
    /// Updates the user interface
    /// </summary>
    /// <remarks>
    /// This is defined here so that UI updates can be accessed from shared.
    /// </remarks>
    public virtual void UpdateUserInterface(Entity<MechComponent> mech)
    {
    }

    private void ToggleMechUi(Entity<MechComponent> mech, EntityUid? user = null)
    {
        user ??= Vehicle.GetOperatorOrNull(mech.Owner);
        if (user == null)
            return;

        _userInterface.TryToggleUi(mech.Owner, MechUiKey.Key, user.Value);
        UpdateUserInterface(mech);
    }

    protected void ReceiveEquipmentUiMessages<T>(Entity<MechComponent> mech, T args) where T : MechEquipmentUiMessage
    {
        var ev = new MechEquipmentUiMessageRelayEvent(args);
        var allEquipment = new List<EntityUid>(mech.Comp.EquipmentContainer.ContainedEntities);
        var argEquip = GetEntity(args.Equipment);

        foreach (var equipment in allEquipment)
        {
            if (argEquip == equipment)
                RaiseLocalEvent(equipment, ev);
        }
    }

    [SubscribeLocalEvent]
    private void OnGetMeleeWeapon(Entity<VehicleOperatorComponent> ent, ref GetMeleeWeaponEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.Vehicle is not { } vehicle)
            return;

        if (!TryComp<MechComponent>(vehicle, out var mech))
            return;

        var weapon = mech.CurrentSelectedEquipment ?? vehicle;
        args.Weapon = weapon;
        args.Handled = true;
    }

    private void UpdateAppearance(Entity<MechComponent> mech, AppearanceComponent? appearance = null)
    {
        if (!Resolve(mech, ref appearance, false))
            return;

        _appearance.SetData(mech.Owner, MechVisuals.Open, !Vehicle.HasOperator(mech.Owner), appearance);
        _appearance.SetData(mech.Owner, MechVisuals.Broken, mech.Comp.Broken, appearance);
    }

    [SubscribeLocalEvent]
    private void OnMechEntryAttempt(Entity<MechComponent> ent, ref ContainerVehicleEntryAttemptEvent args)
    {
        if (ent.Comp.Broken)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnMechExitAttempt(Entity<MechComponent> ent, ref ContainerVehicleExitAttemptEvent args)
    {
        if (ent.Comp.Broken)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnOperatorSet(Entity<MechComponent> ent, ref VehicleOperatorSetEvent args)
    {
        if (args.OldOperator is { } oldOperator)
        {
            RemoveUser(ent, oldOperator);
        }

        if (args.NewOperator is { } newOperator)
        {
            SetupUser(ent, newOperator);
        }

        UpdateAppearance(ent);
        UpdateUserInterface(ent);

        if (args.NewOperator is null && args.OldOperator is not null)
            _userInterface.CloseUi(ent.Owner, MechUiKey.Key);
    }
}

/// <summary>
/// Event raised when the battery is successfully removed from the mech,
/// on both success and failure
/// </summary>
[Serializable, NetSerializable]
public sealed partial class RemoveBatteryEvent : SimpleDoAfterEvent;
