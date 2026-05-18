using Robust.Shared.Prototypes;

namespace Content.Shared.Light.Prototypes;

/// <summary>
/// Defines a type for light bulbs, such as tubes or small bulbs.
/// </summary>
[Prototype]
public sealed partial class LightBulbTypePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public EntProtoId? BrokenPrototype;

    [DataField]
    public EntProtoId? AgedPrototype;

    [DataField]
    public bool FlickersWhenAged;
}
