using System.Linq;
using Content.Server.Construction;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Power.Components;
using Content.Shared.Chat;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Damage.Components;
using Content.Shared.Destructible;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
using Content.Shared.Kitchen.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Kitchen.EntitySystems;

public sealed class MicrowaveSystem : SharedMicrowaveSystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedSuicideSystem _suicide = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly KitchenDeviceSystem _serverKitchenDevice = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MicrowaveComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MicrowaveComponent, SolutionChangedEvent>(OnSolutionChange);
        SubscribeLocalEvent<MicrowaveComponent, InteractUsingEvent>(OnInteractUsing, after: [typeof(AnchorableSystem)]);
        SubscribeLocalEvent<MicrowaveComponent, BreakageEventArgs>(OnBreak);
        SubscribeLocalEvent<MicrowaveComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<MicrowaveComponent, SuicideByEnvironmentEvent>(OnSuicideByEnvironment);

        SubscribeLocalEvent<MicrowaveComponent, SignalReceivedEvent>(OnSignalReceived);

        SubscribeLocalEvent<MicrowaveComponent, MicrowaveStartCookMessage>((u, c, m) => Wzhzhzh(u, c, m.Actor));

        SubscribeLocalEvent<ActiveKitchenDeviceComponent, EntInsertedIntoContainerMessage>(OnActiveMicrowaveInsert);
        SubscribeLocalEvent<ActiveKitchenDeviceComponent, EntRemovedFromContainerMessage>(OnActiveMicrowaveRemove);

        SubscribeLocalEvent<ActivelyMicrowavedComponent, OnConstructionTemperatureEvent>(OnConstructionTemp);
        SubscribeLocalEvent<ActivelyMicrowavedComponent, SolutionRelayEvent<ReactionAttemptEvent>>(OnReactionAttempt);

        SubscribeLocalEvent<FoodRecipeProviderComponent, GetSecretRecipesEvent>(OnGetSecretRecipes);
    }

    protected override void OnCookStart(EntityUid uid, MicrowaveComponent component)
    {
        base.OnCookStart(uid, component);
        SetAppearance(uid, MicrowaveVisualState.Cooking, component);

        _kitchenDevice.StartLoopingSound(uid, component.LoopingSound, ref component.PlayingStream);
    }

    protected override void OnCookStop(EntityUid uid, MicrowaveComponent component)
    {
        base.OnCookStop(uid, component);
        SetAppearance(uid, MicrowaveVisualState.Idle, component);
        _kitchenDevice.StopLoopingSound(ref component.PlayingStream);
    }

    private void OnActiveMicrowaveInsert(Entity<ActiveKitchenDeviceComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        var microwavedComp = AddComp<ActivelyMicrowavedComponent>(args.Entity);
        microwavedComp.Microwave = ent.Owner;
    }

    private void OnActiveMicrowaveRemove(Entity<ActiveKitchenDeviceComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        RemCompDeferred<ActivelyMicrowavedComponent>(args.Entity);
    }

    // Stop items from transforming through constructiongraphs while being microwaved.
    // They might be reserved for a microwave recipe.
    private static void OnConstructionTemp(Entity<ActivelyMicrowavedComponent> ent, ref OnConstructionTemperatureEvent args)
    {
        args.Result = HandleResult.False;
    }

    // Stop reagents from reacting if they are currently reserved for a microwave recipe.
    // For example Egg would cook into EggCooked, causing it to not being removed once we are done microwaving.
    private void OnReactionAttempt(Entity<ActivelyMicrowavedComponent> ent, ref SolutionRelayEvent<ReactionAttemptEvent> args)
    {
        if (!TryComp<ActiveKitchenDeviceComponent>(ent.Comp.Microwave, out var activeKitchenComp))
            return;

        if (activeKitchenComp.RecipeId == null) // no recipe selected
            return;

        if (!_prototype.TryIndex<FoodRecipePrototype>(activeKitchenComp.RecipeId, out var recipe))
            return;

        var recipeReagents = recipe.Ingredients.Reagents.Keys;

        foreach (var reagent in recipeReagents)
        {
            if (!args.Event.Reaction.Reactants.ContainsKey(reagent)) continue;
            args.Event.Cancelled = true;
            return;
        }
    }

    private void OnMapInit(Entity<MicrowaveComponent> ent, ref MapInitEvent args)
    {
        _deviceLink.EnsureSinkPorts(ent, ent.Comp.OnPort);
    }

    /// <summary>
    /// Kills the user by microwaving their head
    /// TODO: Make this not awful, it keeps any items attached to your head still on and you can revive someone and cogni them so you have some dumb headless fuck running around. I've seen it happen.
    /// </summary>
    private void OnSuicideByEnvironment(Entity<MicrowaveComponent> ent, ref SuicideByEnvironmentEvent args)
    {
        if (args.Handled)
            return;

        // The act of getting your head microwaved doesn't actually kill you
        if (!TryComp<DamageableComponent>(args.Victim, out var damageableComponent))
            return;

        // The application of lethal damage is what kills you...
        _suicide.ApplyLethalDamage((args.Victim, damageableComponent), "Heat");

        var victim = args.Victim;

        var othersMessage = Loc.GetString("microwave-component-suicide-others-message", ("victim", victim));
        var selfMessage = Loc.GetString("microwave-component-suicide-message");

        _popupSystem.PopupEntity(othersMessage, victim, Filter.PvsExcept(victim), true);
        _popupSystem.PopupEntity(selfMessage, victim, victim);

        _kitchenDevice.PlaySound(ent, ent.Comp.ClickSound, AudioParams.Default.WithVolume(-2));
        ent.Comp.CurrentCookTimerTime = 10;
        Wzhzhzh(ent.Owner, ent.Comp, args.Victim);
        UpdateUserInterfaceState(ent.Owner, ent.Comp);
        args.Handled = true;
    }

    private void OnSolutionChange(Entity<MicrowaveComponent> ent, ref SolutionChangedEvent args)
    {
        UpdateUserInterfaceState(ent, ent.Comp);
    }

    private void OnInteractUsing(Entity<MicrowaveComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ApcPowerReceiverComponent>(ent, out var apc) || !apc.Powered)
        {
            _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-using-no-power"), ent, args.User);
            return;
        }

        if (ent.Comp.Broken)
        {
            _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-using-broken"), ent, args.User);
            return;
        }

        if (!TryComp<ItemComponent>(args.Used, out _))
        {
            _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-using-transfer-fail"), ent, args.User);
            return;
        }

        // Check if item fits (size + capacity)
        if (!_kitchenDevice.ItemFitsInDevice(ent.Comp.Storage, ent.Comp.Capacity, args.Used, ent.Comp.MaxItemSize))
        {
            _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-item-too-big", ("item", args.Used)), ent, args.User);
            return;
        }

        args.Handled = true;
        _handsSystem.TryDropIntoContainer(args.User, args.Used, ent.Comp.Storage);
        _kitchenDevice.PlayClickSound(ent, ent.Comp.ClickSound, args.User);
        UpdateUserInterfaceState(ent, ent.Comp);
    }

    private void OnBreak(Entity<MicrowaveComponent> ent, ref BreakageEventArgs args)
    {
        ent.Comp.Broken = true;
        SetAppearance(ent, MicrowaveVisualState.Broken, ent.Comp);
        StopCooking(ent);
        _kitchenDevice.EjectAll(ent.Comp.Storage);
        UpdateUserInterfaceState(ent, ent.Comp);
    }

    private void OnAnchorChanged(EntityUid uid, MicrowaveComponent component, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            _kitchenDevice.EjectAll(component.Storage);
    }

    private void OnSignalReceived(Entity<MicrowaveComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port != ent.Comp.OnPort)
            return;

        if (ent.Comp.Broken || !Power.IsPowered(ent.Owner))
            return;

        Wzhzhzh(ent.Owner, ent.Comp, null);
    }

    protected override void UpdateUserInterfaceState(EntityUid uid, MicrowaveComponent component)
    {
        _userInterface.SetUiState(uid, MicrowaveUiKey.Key, new MicrowaveUpdateUserInterfaceState(
            GetNetEntityArray(component.Storage.ContainedEntities.ToArray()),
            HasComp<ActiveKitchenDeviceComponent>(uid),
            component.CurrentCookTimeButtonIndex,
            component.CurrentCookTimerTime,
            component.CurrentCookTimeEnd
        ));
    }

    private void SetAppearance(EntityUid uid, MicrowaveVisualState state, MicrowaveComponent? component = null, AppearanceComponent? appearanceComponent = null)
    {
        if (!Resolve(uid, ref component, ref appearanceComponent, false))
            return;
        var display = component.Broken ? MicrowaveVisualState.Broken : state;
        _appearance.SetData(uid, PowerDeviceVisuals.VisualState, display, appearanceComponent);
    }

    /// <summary>
    /// Explodes the microwave internally, turning it into a broken state.
    /// Wrapper for unified KitchenDeviceSystem.Explode.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    public void Explode(Entity<MicrowaveComponent> ent)
    {
        _serverKitchenDevice.Explode(ent, () => ent.Comp.Broken = true);
    }

    /// <summary>
    ///     Turns a single entity in the microwave into a failed "burned mess" recipe.
    /// </summary>
    /// <remarks>
    ///     This happens to entities that pass <see cref="MicrowaveComponent.BurnWhenCookedWhitelist"/>
    ///     when microwaved.
    /// </remarks>
    /// <param name="microwave">The microwave entity.</param>
    /// <param name="item">The entity to burn.</param>
    private void CreateBurnedMess(Entity<MicrowaveComponent> microwave, EntityUid item)
    {
        _kitchenDevice.ReplaceEntityWithJunk(microwave, item, microwave.Comp.Storage, microwave.Comp.BadRecipeEntityId);
    }

    /// <summary>
    /// Starts Cooking
    /// </summary>
    /// <remarks>
    /// It does not make a "wzhzhzh" sound, it makes a "mmmmmmmm" sound!
    /// -emo
    /// </remarks>
    private void Wzhzhzh(EntityUid uid, MicrowaveComponent component, EntityUid? user)
    {
        if (!_kitchenDevice.CanDeviceBeUsed(uid, component.Storage, component.Broken))
            return;

        // TODO use lists of Reagent quantities instead of reagent prototype ids.
        var context = new MicrowaveProcessingContext(uid, component, user);
        Shared.Kitchen.EntitySystems.KitchenDeviceSystem.ProcessContainerContents(component.Storage, ProcessMicrowaveItem, context);

        if (context.Handled)
        {
            UpdateUserInterfaceState(uid, component);
            return;
        }

        // Check recipes
        var portionedRecipe = _serverKitchenDevice.FindBestRecipeForDevice(uid, component.Storage, component.CurrentCookTimerTime);

        ActivateMicrowave((uid, component), portionedRecipe, context.Malfunctioning);
    }

    private bool ProcessMicrowaveItem(EntityUid item, MicrowaveProcessingContext ctx)
    {
        // special behavior when being microwaved ;)
        var ev = new BeingMicrowavedEvent(ctx.Uid, ctx.User);
        RaiseLocalEvent(item, ev);

        if (ev.Handled)
        {
            ctx.Handled = true;
            return false; // Stop processing
        }

        if (_whitelist.IsWhitelistPass(ctx.Component.MalfunctionWhenCookedWhitelist, item))
        {
            ctx.Malfunctioning = true;
        }

        if (_whitelist.IsWhitelistPass(ctx.Component.BurnWhenCookedWhitelist, item))
        {
            CreateBurnedMess((ctx.Uid, ctx.Component), item);
            return true; // Continue with next item
        }

        var microwavedComp = AddComp<ActivelyMicrowavedComponent>(item);
        microwavedComp.Microwave = ctx.Uid;
        return true;
    }

    /// <summary>
    /// Context for microwave item processing.
    /// </summary>
    private sealed class MicrowaveProcessingContext(
        EntityUid uid,
        MicrowaveComponent component,
        EntityUid? user)
    {
        public readonly EntityUid Uid = uid;
        public readonly MicrowaveComponent Component = component;
        public readonly EntityUid? User = user;
        public bool Malfunctioning;
        public bool Handled;
    }

    /// <summary>
    ///     Starts up the microwave cooking operation, setting the starting time and recipe of the microwave.
    /// </summary>
    private void ActivateMicrowave(Entity<MicrowaveComponent> microwave,
        (FoodRecipePrototype? recipe, int count) recipe,
        bool malfunctioning)
    {
        var uid = microwave.Owner;
        var component = microwave.Comp;

        _kitchenDevice.PlayStartSound(uid, component.StartCookingSound);

        var cookTime = component.CurrentCookTimerTime;
        var scaledTime = cookTime * component.CookTimeMultiplier;

        _kitchenDevice.StartTimer(uid, cookTime, component.CookTimeMultiplier, "cook");
        _serverKitchenDevice.SetRecipeData(uid, recipe.recipe?.ID, recipe.count);

        //Scale times with cook times
        component.CurrentCookTimeEnd = Timing.CurTime + TimeSpan.FromSeconds(scaledTime);

        if (malfunctioning)
            _kitchenDevice.SetMalfunctionTime(uid, component.MalfunctionInterval);

        UpdateUserInterfaceState(uid, component);
    }

    protected override void StopCooking(Entity<MicrowaveComponent> ent)
    {
        _kitchenDevice.StopTimer(ent);

        foreach (var solid in ent.Comp.Storage.ContainedEntities)
        {
            RemCompDeferred<ActivelyMicrowavedComponent>(solid);
        }
    }

    protected override void OnCookingComplete(EntityUid uid, ActiveKitchenDeviceComponent active, MicrowaveComponent microwave, float remainingHeatTime)
    {
        // Add remaining heat
        _kitchenDevice.AddTemperature(
            microwave.Storage,
            remainingHeatTime,
            microwave.BaseHeatMultiplier,
            microwave.ObjectHeatMultiplier,
            microwave.TemperatureUpperThreshold);

        // Process recipe if there is one
        if (active.RecipeId != null)
        {
            var coords = Transform(uid).Coordinates;
            _serverKitchenDevice.SubtractRecipeIngredients(microwave.Storage, active.RecipeId, (uint)active.PortionCount);
            _serverKitchenDevice.SpawnRecipeResults(active.RecipeId, active.PortionCount, coords);
        }

        _kitchenDevice.EjectAll(microwave.Storage);
        _kitchenDevice.PlayDoneSound(uid, microwave.FoodDoneSound);

        base.OnCookingComplete(uid, active, microwave, remainingHeatTime);
    }

    /// <summary>
    /// This event tries to get secret recipes that the microwave might be capable of.
    /// Currently, we only check the microwave itself, but in the future, the user might be able to learn recipes.
    /// </summary>
    private void OnGetSecretRecipes(Entity<FoodRecipeProviderComponent> ent, ref GetSecretRecipesEvent args)
    {
        _serverKitchenDevice.CollectSecretRecipes(ent, ref args);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveKitchenDeviceComponent, MicrowaveComponent>();
        while (query.MoveNext(out var uid, out var active, out var microwave))
        {
            _serverKitchenDevice.RollMalfunction<MicrowaveComponent>(
                (uid, active, microwave),
                microwave.ExplosionChance,
                microwave.LightningChance,
                microwave.MalfunctionInterval,
                microwave.MalfunctionSpark);
        }
    }

    protected override void OnPowerChanged(Entity<MicrowaveComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            SetAppearance(ent, MicrowaveVisualState.Idle, ent.Comp);
            StopCooking(ent);
        }
        UpdateUserInterfaceState(ent, ent.Comp);
    }
}
