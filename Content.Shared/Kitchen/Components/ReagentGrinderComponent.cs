using Content.Shared.Kitchen.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Kitchen.Components;

/// <summary>
/// The combo reagent grinder/juicer. The reason why grinding and juicing are seperate is simple,
/// think of grinding as a utility to break an object down into its reagents. Think of juicing as
/// converting something into its single juice form. E.g, grind an apple and get the nutriment and sugar
/// it contained, juice an apple and get "apple juice".
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true), AutoGenerateComponentPause]
[Access(typeof(SharedReagentGrinderSystem))]
public sealed partial class ReagentGrinderComponent : Component
{
    public const string BeakerSlotId = "beakerSlot";

    public const string InputContainerId = "inputContainer";

    [ViewVariables]
    public Container InputContainer = default!;

    [DataField, AutoNetworkedField]
    public int StorageMaxEntities = 6;

    /// <summary>
    /// The time grinding or juicing takes.
    /// Roughly matches the grind/juice sounds.
    /// </summary>
    [DataField]
    public TimeSpan WorkTime = TimeSpan.FromSeconds(3.5f);

    /// <summary>
    /// Multiplier for WorkTime, that pitches the audio accordingly.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float WorkTimeMultiplier = 1.0f;

    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg", AudioParams.Default.WithVolume(-2f));

    [DataField]
    public SoundSpecifier GrindSound = new SoundPathSpecifier("/Audio/Machines/blender.ogg");

    [DataField]
    public SoundSpecifier JuiceSound = new SoundPathSpecifier("/Audio/Machines/juicer.ogg");

    [DataField, AutoNetworkedField]
    public GrinderAutoMode AutoMode = GrinderAutoMode.Off;

    [DataField]
    public EntityUid? AudioStream;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan? EndTime;

    [DataField, AutoNetworkedField]
    public float TimeRemaining;

    [DataField, AutoNetworkedField]
    public float TotalTime;

    [DataField, AutoNetworkedField]
    public GrinderProgram? Program;

    [ViewVariables]
    public bool IsOperating => EndTime.HasValue;
}

/// <summary>
/// Marker component for active reagent grinders used to improve the EntityQueryEnumerator performance in the update loop.
/// If you want to check if the grinder is currently active use <see cref="SharedReagentGrinderSystem.IsActive"/> instead,
/// because this component is being removed deferred, i.e. in the following game tick.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveReagentGrinderComponent : Component;

/// <summary>
/// Marker component added to beakers inserted into a reagent grinder.
/// Used to update the UI when the solution in the beaker changes.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class InsideReagentGrinderComponent : Component;
