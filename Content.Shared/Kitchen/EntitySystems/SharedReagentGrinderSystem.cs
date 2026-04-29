using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.Interaction;
using Content.Shared.Jittering;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Kitchen.EntitySystems;

[UsedImplicitly]
public abstract class SharedReagentGrinderSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainersSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly KitchenDeviceSystem _kitchenDevice = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InsideReagentGrinderComponent, SolutionChangedEvent>(OnBeakerSolutionContainerChanged);

        SubscribeLocalEvent<ReagentGrinderComponent, ComponentStartup>(OnGrinderStartup);
        SubscribeLocalEvent<ReagentGrinderComponent, ContainerIsRemovingAttemptEvent>(OnEntRemovingAttempt);
        SubscribeLocalEvent<ReagentGrinderComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<ReagentGrinderComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent((EntityUid uid, ReagentGrinderComponent _, ref PowerChangedEvent _) => UpdateUi(uid));
        SubscribeLocalEvent<ReagentGrinderComponent, InteractUsingEvent>(OnInteractUsing);

        SubscribeLocalEvent<ReagentGrinderComponent, ReagentGrinderStartMessage>(OnStartMessage);
        SubscribeLocalEvent<ReagentGrinderComponent, ReagentGrinderToggleAutoModeMessage>(OnToggleAutoModeMessage);
        SubscribeLocalEvent<ReagentGrinderComponent, ReagentGrinderEjectChamberAllMessage>(OnEjectChamberAllMessage);
        SubscribeLocalEvent<ReagentGrinderComponent, ReagentGrinderEjectChamberContentMessage>(OnEjectChamberContentMessage);
    }

    private void OnBeakerSolutionContainerChanged(Entity<InsideReagentGrinderComponent> ent, ref SolutionChangedEvent args)
    {
        // Update the UI if the reagents inside the beaker are changed.
        // This is needed in case the component state for the container is applied before that of the solution container
        // or if the beaker somehow changes its contents on its own (for with example SolutionRegenerationComponent).
        UpdateUi(Transform(ent).ParentUid);
    }

    private void OnGrinderStartup(Entity<ReagentGrinderComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.InputContainer = _containerSystem.EnsureContainer<Container>(ent.Owner, ReagentGrinderComponent.InputContainerId);
    }

    private void OnEntRemovingAttempt(Entity<ReagentGrinderComponent> ent, ref ContainerIsRemovingAttemptEvent args)
    {
        // Allow server states to be applied without cancelling container changes.
        if (_timing.ApplyingState)
            return;

        if (args.Container.ID != ReagentGrinderComponent.BeakerSlotId
            && args.Container.ID != ReagentGrinderComponent.InputContainerId)
            return;

        // Cannot remove items while the grinder is active.
        if (_kitchenDevice.IsActive(ent))
            args.Cancel();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveKitchenDeviceComponent, ReagentGrinderComponent>();
        while (query.MoveNext(out var uid, out _, out var grinderComp))
        {
            if (!_kitchenDevice.ProcessTimer(uid, frameTime, out _))
                continue;

            FinishGrinding((uid, grinderComp));
        }
    }

    private void OnEntRemoved(EntityUid uid, ReagentGrinderComponent comp, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ReagentGrinderComponent.BeakerSlotId
            && args.Container.ID != ReagentGrinderComponent.InputContainerId)
            return;

        // Always update the UI on the client, both during prediction and when applying a game state.
        UpdateUi(uid);

        // The component changes from the code below are already part of the same game state being applied.
        if (_timing.ApplyingState)
            return;

        if (args.Container.ID != ReagentGrinderComponent.BeakerSlotId) // Beaker removed.
            return;

        RemComp<InsideReagentGrinderComponent>(args.Entity);
        _appearanceSystem.SetData(uid, ReagentGrinderVisualState.BeakerAttached, false);
    }

    private void OnEntInserted(EntityUid uid, ReagentGrinderComponent comp, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ReagentGrinderComponent.BeakerSlotId
            && args.Container.ID != ReagentGrinderComponent.InputContainerId)
            return;

        // Always update the UI on the client, both during prediction and when applying a game state.
        UpdateUi(uid);

        // The component changes from the code below are already part of the same game state being applied.
        if (_timing.ApplyingState)
            return;

        if (args.Container.ID == ReagentGrinderComponent.BeakerSlotId) // Beaker inserted.
        {
            EnsureComp<InsideReagentGrinderComponent>(args.Entity);
            _appearanceSystem.SetData(uid, ReagentGrinderVisualState.BeakerAttached, true);
        }

        // Start grinder when in auto mode.
        if (comp.AutoMode == GrinderAutoMode.Off)
            return;

        var program = comp.AutoMode == GrinderAutoMode.Grind ? GrinderProgram.Grind : GrinderProgram.Juice;
        StartGrinder((uid, comp), program);
    }

    private void OnInteractUsing(Entity<ReagentGrinderComponent> ent, ref InteractUsingEvent args)
    {
        var heldEnt = args.Used;

        if (!HasComp<ExtractableComponent>(heldEnt))
        {
            if (!HasComp<FitsInDispenserComponent>(heldEnt))
            {
                // This is ugly but we can't use whitelistFailPopup because there are 2 containers with different whitelists.
                _popupSystem.PopupClient(Loc.GetString("reagent-grinder-component-cannot-put-entity-message"), ent.Owner, args.User);
            }

            // Entity did NOT pass the whitelist for grind/juice.
            // Wouldn't want the clown grinding up the Captain's ID card now would you?
            // Why am I asking you? You're biased.
            return;
        }

        if (args.Handled)
            return;

        // Cap the chamber. Don't want someone putting in 500 entities and ejecting them all at once.
        if (!_kitchenDevice.CanInsertItem(ent, heldEnt, ent.Comp.InputContainer, ent.Comp.StorageMaxEntities))
        {
            _popupSystem.PopupClient(Loc.GetString("reagent-grinder-component-interact-full"), ent.Owner, args.User);
            return;
        }

        if (!_containerSystem.Insert(heldEnt, ent.Comp.InputContainer))
            return;

        args.Handled = true;
        _kitchenDevice.PlayClickSound(ent, ent.Comp.ClickSound, args.User);
    }

    /// <summary>
    /// Update the reagent grinder BUI for the client.
    /// </summary>
    protected virtual void UpdateUi(EntityUid uid) { }

    private void OnStartMessage(Entity<ReagentGrinderComponent> ent, ref ReagentGrinderStartMessage message)
    {
        StartGrinder(ent, message.Program);
    }

    private void OnToggleAutoModeMessage(Entity<ReagentGrinderComponent> ent, ref ReagentGrinderToggleAutoModeMessage message)
    {
        // Cycle through the enum values.
        ent.Comp.AutoMode = (GrinderAutoMode)(((byte)ent.Comp.AutoMode + 1) % Enum.GetValues<GrinderAutoMode>().Length);
        Dirty(ent);

        _kitchenDevice.PlayClickSound(ent, ent.Comp.ClickSound, message.Actor);
        UpdateUi(ent);
    }

    private void OnEjectChamberAllMessage(Entity<ReagentGrinderComponent> ent, ref ReagentGrinderEjectChamberAllMessage message)
    {
        if (_kitchenDevice.IsActive(ent))
            return;

        _kitchenDevice.HandleEjectAll(ent, ent.Comp.InputContainer, ent.Comp.ClickSound, message.Actor);
        // UpdateUi is called in the resulting ContainerModifiedMessage.
    }

    private void OnEjectChamberContentMessage(Entity<ReagentGrinderComponent> ent, ref ReagentGrinderEjectChamberContentMessage message)
    {
        if (_kitchenDevice.IsActive(ent))
            return;

        if (!TryGetEntity(message.EntityId, out var toRemove))
            return;

        _kitchenDevice.HandleEjectItem(ent, toRemove.Value, ent.Comp.InputContainer, ent.Comp.ClickSound, message.Actor);
        // UpdateUi is called in the resulting ContainerModifiedMessage.
    }

    /// <summary>
    /// The wzhzhzh of the grinder. Marks the grinder as active, but does not convert the items into reagents yet.
    /// </summary>
    private void StartGrinder(Entity<ReagentGrinderComponent> ent, GrinderProgram program)
    {
        if (!_kitchenDevice.CanDeviceBeUsed(ent, ent.Comp.InputContainer, false))
            return;

        var beaker = _itemSlotsSystem.GetItemOrNull(ent, ReagentGrinderComponent.BeakerSlotId);

        // Do we have anything to grind/juice and a container to put the reagents in?
        if (!HasComp<FitsInDispenserComponent>(beaker))
            return;

        switch (program)
        {
            case GrinderProgram.Grind when !ent.Comp.InputContainer.ContainedEntities.All(x => CanGrind(x)):
                return;
            case GrinderProgram.Juice when !ent.Comp.InputContainer.ContainedEntities.All(x => CanJuice(x)):
                return;
        }

        var modeString = program.ToString().ToLowerInvariant();
        var modeSounds = new Dictionary<string, SoundSpecifier>
        {
            { "grind", ent.Comp.GrindSound },
            { "juice", ent.Comp.JuiceSound }
        };

        var workTimeSeconds = (float)ent.Comp.WorkTime.TotalSeconds;
        _kitchenDevice.StartTimer(ent, workTimeSeconds, ent.Comp.WorkTimeMultiplier, modeString);

        // Store mode for later retrieval
        if (TryComp<ActiveKitchenDeviceComponent>(ent, out var activeComp))
        {
            activeComp.Mode = modeString;
        }

        _jitter.AddJitter(ent, -10, 100);
        _kitchenDevice.SetWorkingState(ent.Owner, true);
        ent.Comp.Program = program;
        Dirty(ent);
        UpdateUi(ent);

        // Unpredicted because we don't have the user in the update loop
        // TODO: Make the audio API sane https://github.com/space-wizards/RobustToolbox/issues/6436
        if (_net.IsServer)
        {
            _kitchenDevice.StartModeLoopingSound(ent, modeSounds, modeString, ref ent.Comp.AudioStream, ent.Comp.WorkTimeMultiplier);
        }
    }

    /// <summary>
    /// Converts items into reagents and marks the grinder as inactive.
    /// </summary>
    private void FinishGrinding(Entity<ReagentGrinderComponent> ent)
    {
        if (ent.Comp.Program is not { } program)
            return; // Already finished.

        ent.Comp.Program = null;
        _kitchenDevice.StopLoopingSound(ref ent.Comp.AudioStream);
        ent.Comp.EndTime = null; // It's important that we do this first or PredictedQueueDelete will fail to remove the entity from the container because the grinder is still active.
        Dirty(ent);
        // Remove deferred to avoid modifying the component we are currently enumerating over in the update loop.
        _kitchenDevice.StopTimer(ent);
        RemCompDeferred<JitteringComponent>(ent);
        _kitchenDevice.SetWorkingState(ent.Owner, false);

        var beaker = _itemSlotsSystem.GetItemOrNull(ent.Owner, ReagentGrinderComponent.BeakerSlotId);
        if (beaker is null || !_solutionContainersSystem.TryGetFitsInDispenser(beaker.Value, out var beakerSolutionEntity, out var beakerSolution))
            return;

        var context = new GrinderProcessingContext(program, beakerSolutionEntity, beakerSolution, _solutionContainersSystem, _destructible, _kitchenDevice);
        KitchenDeviceSystem.ProcessContainerContents(ent.Comp.InputContainer, ProcessGrinderItem, context);
        // UpdateUi is called when the entity in the grinder is deleted or the solution in the beaker is changed.
    }

    private bool ProcessGrinderItem(EntityUid item, GrinderProcessingContext ctx)
    {
        var solution = GetGrinderSolution(item, ctx.Program);
        if (solution is null)
            return true; // Continue with next item

        // Handle stackable items
        if (TryComp<StackComponent>(item, out _))
        {
            if (!ctx.KitchenDevice.ProcessStackWithScaling(item, solution, ctx.BeakerSolution.AvailableVolume,
                    out var processedSolution, out _))
                return true; // Continue with next item

            solution = processedSolution!;
        }
        else
        {
            if (solution.Volume > ctx.BeakerSolution.AvailableVolume)
                return true; // Continue with next item

            ctx.Destructible.DestroyEntity(item);
        }

        if (ctx.BeakerSolutionEntity is {} solnEnt)
            ctx.SolutionContainers.TryAddSolution(solnEnt, solution);

        return true;
    }

    /// <summary>
    /// Context for grinder item processing.
    /// </summary>
    private sealed class GrinderProcessingContext(
        GrinderProgram program,
        Entity<SolutionComponent>? beakerSolutionEntity,
        Solution beakerSolution,
        SharedSolutionContainerSystem solutionContainers,
        SharedDestructibleSystem destructible,
        KitchenDeviceSystem kitchenDevice)
    {
        public readonly GrinderProgram Program = program;
        public readonly Entity<SolutionComponent>? BeakerSolutionEntity = beakerSolutionEntity;
        public readonly Solution BeakerSolution = beakerSolution;
        public readonly SharedSolutionContainerSystem SolutionContainers = solutionContainers;
        public readonly SharedDestructibleSystem Destructible = destructible;
        public readonly KitchenDeviceSystem KitchenDevice = kitchenDevice;
    }

    /// <summary>
    /// Is the given grinder currently grinding/juicing?
    /// </summary>
    public bool IsActive(Entity<ReagentGrinderComponent?> ent)
    {
        // Don't use ActiveGrinderComponent for this because it is being removed deferred, meaning it will get updated at the end of the tick.
        // ActiveReagentGrinderComponent is only for improving the EntityQueryEnumerator performance in the update loop.
        return Resolve(ent, ref ent.Comp) && _kitchenDevice.IsActive(ent);
    }

    /// <summary>
    /// Gets the solutions from an entity using the specified Grinder program.
    /// </summary>
    /// <param name="ent">The entity which we check for solutions.</param>
    /// <param name="program">The grinder program.</param>
    /// <returns>The solution received, or null if none.</returns>
    public Solution? GetGrinderSolution(Entity<ExtractableComponent?> ent, GrinderProgram program)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return null;

        switch (program)
        {
            case GrinderProgram.Grind:
                if (ent.Comp.GrindableSolutionName is not { } solutionId)
                    return null;

                if (_solutionContainersSystem.TryGetSolution(ent.Owner, solutionId, out _, out var solution))
                    return solution;
                break;
            case GrinderProgram.Juice:
                return ent.Comp.JuiceSolution;
        }

        return null;
    }

    /// <summary>
    /// Checks whether the entity can be ground using a ReagentGrinder.
    /// </summary>
    /// <param name="ent">The entity to check.</param>
    /// <returns>True if it can be ground, otherwise false.</returns>
    /// <remarks>
    /// Will it blend? That is the question!
    /// </remarks>
    public bool CanGrind(Entity<ExtractableComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (ent.Comp.GrindableSolutionName == null)
            return false;

        return _solutionContainersSystem.TryGetSolution(ent.Owner, ent.Comp.GrindableSolutionName, out _, out _);
    }

    /// <summary>
    /// Checks whether the entity can be juiced using a ReagentGrinder.
    /// </summary>
    /// <param name="ent">The entity to check.</param>
    /// <returns>True if it can be juiced, otherwise false.</returns>
    public bool CanJuice(Entity<ExtractableComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        return ent.Comp.JuiceSolution is not null;
    }
}
