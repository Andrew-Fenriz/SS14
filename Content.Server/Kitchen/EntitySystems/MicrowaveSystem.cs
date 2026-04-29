using System.Linq;
using Content.Server.Construction;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Kitchen.Components;
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
using Robust.Shared.Audio.Systems;
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
    [Dependency] private new readonly KitchenDeviceSystem _kitchenDevice = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

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

        SubscribeLocalEvent<FoodRecipeProviderComponent, GetSecretRecipesEvent>(OnGetSecretRecipes);

        SubscribeLocalEvent<ActivelyMicrowavedComponent, OnConstructionTemperatureEvent>(OnConstructionTemp);
        SubscribeLocalEvent<ActivelyMicrowavedComponent, SolutionRelayEvent<ReactionAttemptEvent>>(OnReactionAttempt);
    }

    protected override void OnCookStart(EntityUid uid, MicrowaveComponent component)
    {
        base.OnCookStart(uid, component);
        SetAppearance(uid, MicrowaveVisualState.Cooking, component);

        component.PlayingStream = _audio.PlayPvs(component.LoopingSound, uid,
            AudioParams.Default.WithLoop(true).WithMaxDistance(5))?.Entity;
    }

    protected override void OnCookStop(EntityUid uid, MicrowaveComponent component)
    {
        base.OnCookStop(uid, component);
        SetAppearance(uid, MicrowaveVisualState.Idle, component);
        component.PlayingStream = _audio.Stop(component.PlayingStream);
    }

    private void OnMapInit(Entity<MicrowaveComponent> ent, ref MapInitEvent args)
    {
        _deviceLink.EnsureSinkPorts(ent, ent.Comp.OnPort);
    }

    // Kills the user by microwaving their head
    // TODO: Make this not awful, it keeps any items attached to your head still on and you can revive someone and cogni them so you have some dumb headless fuck running around. I've seen it happen.
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

        _audio.PlayPvs(ent.Comp.ClickSound, ent, AudioParams.Default.WithVolume(-2));
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

        // check if thing you're trying to put in isn't an item
        if (!TryComp<ItemComponent>(args.Used, out _))
        {
            _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-using-transfer-fail"), ent, args.User);
            return;
        }

        // check if size of an item you're trying to put in is too big
        if (!_kitchenDevice.ItemFitsInDevice(ent.Comp.Storage, ent.Comp.Capacity, args.Used, ent.Comp.MaxItemSize))
        {
            _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-item-too-big", ("item", args.Used)), ent, args.User);
            return;
        }

        args.Handled = true;
        _handsSystem.TryDropIntoContainer(args.User, args.Used, ent.Comp.Storage);
        _audio.PlayPredicted(ent.Comp.ClickSound, ent, args.User);
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
            component.IsOperating,
            component.CurrentCookTimeButtonIndex,
            component.CurrentCookTimerTime,
            component.EndTime ?? TimeSpan.Zero
        ));
    }

    private void SetAppearance(EntityUid uid, MicrowaveVisualState state, MicrowaveComponent? component = null, AppearanceComponent? appearanceComponent = null)
    {
        if (!Resolve(uid, ref component, ref appearanceComponent, false))
            return;
        var display = component.Broken ? MicrowaveVisualState.Broken : state;
        _appearance.SetData(uid, PowerDeviceVisuals.VisualState, display, appearanceComponent);
    }

    // Explodes the microwave internally, turning it into a broken state.
    public void Explode(Entity<MicrowaveComponent> ent)
    {
        _kitchenDevice.Explode(ent, () => ent.Comp.Broken = true);
    }

    // Turns a single entity in the microwave into a failed "burned mess" recipe.
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
        if (component.Broken)
            return;

        if (component.IsOperating)
            return;

        if (!Power.IsPowered(uid))
            return;

        if (!Shared.Kitchen.EntitySystems.KitchenDeviceSystem.HasContents(component.Storage))
            return;

        // TODO: Use ReagentQuantity lists instead of reagent prototype ids.
        var context = new MicrowaveProcessingContext(uid, component, user);
        _kitchenDevice.ProcessContainerContents(component.Storage, ProcessMicrowaveItem, context);

        if (context.Handled)
        {
            UpdateUserInterfaceState(uid, component);
            return;
        }

        var portionedRecipe = _kitchenDevice.FindBestRecipeForDevice(uid, component.Storage, component.CurrentCookTimerTime);

        ActivateMicrowave((uid, component), portionedRecipe, context.Malfunctioning);
    }

    private bool ProcessMicrowaveItem(EntityUid item, MicrowaveProcessingContext ctx)
    {
        // Allow entities to react to being microwaved.
        // TODO: Stop items from transforming through constructiongraphs while being microwaved.
        // TODO: Stop reagents from reacting if they are currently reserved for a microwave recipe.
        var ev = new BeingMicrowavedEvent(ctx.Uid, ctx.User);
        RaiseLocalEvent(item, ev);

        if (ev.Handled)
        {
            ctx.Handled = true;
            return false;
        }

        if (_whitelist.IsWhitelistPass(ctx.Component.MalfunctionWhenCookedWhitelist, item))
        {
            ctx.Malfunctioning = true;
        }

        if (_whitelist.IsWhitelistPass(ctx.Component.BurnWhenCookedWhitelist, item))
        {
            CreateBurnedMess((ctx.Uid, ctx.Component), item);
        }

        return true;
    }

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

    private void ActivateMicrowave(Entity<MicrowaveComponent> microwave,
        (FoodRecipePrototype? recipe, int count) recipe,
        bool malfunctioning)
    {
        var uid = microwave.Owner;
        var component = microwave.Comp;

        _audio.PlayPvs(component.StartCookingSound, uid);

        var cookTime = component.CurrentCookTimerTime;
        var scaledTime = cookTime * component.CookTimeMultiplier;

        component.TotalTime = cookTime;
        component.TimeRemaining = cookTime;
        component.EndTime = Timing.CurTime + TimeSpan.FromSeconds(scaledTime);
        component.RecipeId = recipe.recipe?.ID;
        component.PortionCount = recipe.count;

        if (malfunctioning)
            _kitchenDevice.EnableMalfunctionChecking(uid);

        EnsureComp<ActiveMicrowaveComponent>(uid);

        foreach (var item in component.Storage.ContainedEntities.ToArray())
        {
            var microwavedComp = AddComp<ActivelyMicrowavedComponent>(item);
            microwavedComp.Microwave = uid;
        }

        OnCookStart(uid, component);
        UpdateUserInterfaceState(uid, component);
    }

    protected override void StopCooking(Entity<MicrowaveComponent> ent)
    {
        ent.Comp.EndTime = null;
        ent.Comp.TimeRemaining = 0;
        ent.Comp.TotalTime = 0;

        foreach (var item in ent.Comp.Storage.ContainedEntities.ToArray())
        {
            RemCompDeferred<ActivelyMicrowavedComponent>(item);
        }

        RemCompDeferred<ActiveMicrowaveComponent>(ent);
        OnCookStop(ent.Owner, ent.Comp);
        _kitchenDevice.DisableMalfunctionChecking(ent.Owner);
    }

    protected override void OnCookingComplete(EntityUid uid, MicrowaveComponent microwave, float remainingHeatTime)
    {
        _kitchenDevice.AddTemperature(
            microwave.Storage,
            remainingHeatTime,
            microwave.BaseHeatMultiplier,
            microwave.ObjectHeatMultiplier,
            microwave.TemperatureUpperThreshold);

        if (microwave.RecipeId != null)
        {
            var coords = Transform(uid).Coordinates;
            _kitchenDevice.SubtractRecipeIngredients(microwave.Storage, microwave.RecipeId, (uint)microwave.PortionCount);
            _kitchenDevice.SpawnRecipeResults(microwave.RecipeId, microwave.PortionCount, coords);
        }

        microwave.RecipeId = null;
        microwave.PortionCount = 0;

        _kitchenDevice.EjectAll(microwave.Storage);
        _audio.PlayPvs(microwave.FoodDoneSound, uid);

        base.OnCookingComplete(uid, microwave, remainingHeatTime);
    }

    // This event tries to get secret recipes that the microwave might be capable of.
    // Currently, we only check the microwave itself, but in the future, the user might be able to learn recipes.
    private void OnGetSecretRecipes(Entity<FoodRecipeProviderComponent> ent, ref GetSecretRecipesEvent args)
    {
        _kitchenDevice.CollectSecretRecipes(ent, ref args);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var malfunctionQuery = EntityQueryEnumerator<MalfunctionComponent>();
        while (malfunctionQuery.MoveNext(out var uid, out var malfunction))
        {
            _kitchenDevice.RollMalfunction((uid, malfunction));
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
        if (!TryComp<MicrowaveComponent>(ent.Comp.Microwave, out var microwaveComp))
            return;

        if (microwaveComp.RecipeId == null) // no recipe selected
            return;

        if (!_prototype.TryIndex<FoodRecipePrototype>(microwaveComp.RecipeId, out var recipe))
            return;

        var recipeReagents = recipe.Ingredients.Reagents.Keys.Select(r => r.Id).ToHashSet();
        var reactionReactants = args.Event.Reaction.Reactants.Keys;
        if (reactionReactants.Any(recipeReagents.Contains))
        {
            args.Event.Cancelled = true;
        }
    }
}
