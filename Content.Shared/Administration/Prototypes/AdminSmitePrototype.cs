using Content.Shared.Administration.Smites;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Administration.Prototypes;

/// <summary>
/// Describes a declarative admin smite.
/// </summary>
[Prototype("adminSmite")]
public sealed partial class AdminSmitePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The localized name shown for the smite.
    /// </summary>
    [DataField(required: true)]
    public LocId Name { get; private set; }

    /// <summary>
    /// The localized description shown for the smite.
    /// </summary>
    [DataField]
    public LocId? Description { get; private set; }

    /// <summary>
    /// The optional icon shown for the smite.
    /// </summary>
    [DataField]
    public SpriteSpecifier? Icon { get; private set; }

    /// <summary>
    /// Restricts which entities the smite is available for.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist { get; private set; }

    /// <summary>
    /// Operations performed by the smite, in order.
    /// </summary>
    [DataField(required: true, serverOnly: true)]
    public SmiteOperation[] Operations { get; private set; } = [];
}
