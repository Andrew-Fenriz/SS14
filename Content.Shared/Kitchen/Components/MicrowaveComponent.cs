using Content.Shared.DeviceLinking;
using Content.Shared.Item;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Kitchen.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MicrowaveComponent : Component
{
    [DataField, AutoNetworkedField]
    public float CookTimeMultiplier = 1;

    [DataField, AutoNetworkedField]
    public float BaseHeatMultiplier = 100;

    [DataField, AutoNetworkedField]
    public float ObjectHeatMultiplier = 100;

    [DataField("failureResult", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string BadRecipeEntityId = "FoodBadRecipe";

    #region audio
    [DataField("beginCookingSound")]
    public SoundSpecifier StartCookingSound = new SoundPathSpecifier("/Audio/Machines/microwave_start_beep.ogg");

    [DataField]
    public SoundSpecifier FoodDoneSound = new SoundPathSpecifier("/Audio/Machines/microwave_done_beep.ogg");

    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid? PlayingStream;

    [DataField]
    public SoundSpecifier LoopingSound = new SoundPathSpecifier("/Audio/Machines/microwave_loop.ogg");
    #endregion

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Broken;

    [DataField]
    public ProtoId<SinkPortPrototype> OnPort = "On";

    /// <summary>
    /// This is a fixed offset of 5.
    /// The cook times for all recipes should be divisible by 5,with a minimum of 1 second.
    /// For right now, I don't think any recipe cook time should be greater than 60 seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public uint CurrentCookTimerTime;

    [DataField, AutoNetworkedField]
    public TimeSpan CurrentCookTimeEnd = TimeSpan.Zero;

    /// <summary>
    /// The maximum number of seconds a microwave can be set to.
    /// This is currently only used for validation and the client does not check this.
    /// </summary>
    [DataField]
    public uint MaxCookTime = 30;

    /// <summary>
    ///     The max temperature that this microwave can heat objects to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TemperatureUpperThreshold = 373.15f;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public int CurrentCookTimeButtonIndex;

    #region Operating State

    [ViewVariables]
    public bool IsOperating => EndTime.HasValue;

    [DataField]
    public float TimeRemaining;

    [DataField]
    public float TotalTime;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? EndTime;

    [DataField]
    public string? RecipeId;

    [DataField]
    public int PortionCount;

    #endregion

    [ViewVariables]
    public Container Storage = default!;

    [DataField]
    public string ContainerId = "microwave_entity_container";

    [DataField]
    public int Capacity = 10;

    [DataField]
    public ProtoId<ItemSizePrototype> MaxItemSize = "Normal";

    /// <summary>
    /// If this microwave can give ids accesses without exploding
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanMicrowaveIdsSafely = true;

    /// <summary>
    /// Entities that fulfill this whitelist will cause the microwave to malfunction
    /// on activation. By default, this is metal objects.
    /// </summary>
    [DataField]
    public EntityWhitelist? MalfunctionWhenCookedWhitelist;

    /// <summary>
    /// Entities that fulfill this whitelist will create a burned mess when microwaved.
    /// By default, this is plastic objects.
    /// </summary>
    [DataField]
    public EntityWhitelist? BurnWhenCookedWhitelist;
}

/// <summary>
/// Marker component for active microwaves used to improve the EntityQueryEnumerator performance in the update loop.
/// If you want to check if the microwave is currently cooking use <see cref="MicrowaveComponent.IsOperating"/> instead,
/// because this component is being removed deferred, i.e. in the following game tick.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveMicrowaveComponent : Component;
