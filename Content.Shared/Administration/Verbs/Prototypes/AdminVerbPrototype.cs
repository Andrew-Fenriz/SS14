using Content.Shared.Administration.Verbs.Operations;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Administration.Verbs.Prototypes;

/// <summary>
/// Common presentation, applicability, and execution data for prototype-backed administrative entity verbs.
/// Registration policy such as permissions, verb category, and logging impact is defined by the system
/// that exposes the concrete verb type.
/// </summary>
[DataDefinition]
public abstract partial class AdminVerbPrototype : IPrototype
{
    /// <inheritdoc />
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Localized text displayed for the verb.
    /// </summary>
    [DataField(required: true)]
    public LocId Name { get; private set; }

    /// <summary>
    /// Optional localized description displayed with the verb.
    /// </summary>
    [DataField]
    public LocId? Description { get; private set; }

    /// <summary>
    /// Optional icon displayed for the verb.
    /// </summary>
    [DataField]
    public SpriteSpecifier? Icon { get; private set; }

    /// <summary>
    /// If specified, the target must match this whitelist for the verb to be available.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist { get; private set; }

    /// <summary>
    /// If specified, matching targets are excluded even if they also match <see cref="Whitelist"/>.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist { get; private set; }

    /// <summary>
    /// Ordered operations executed on the target when the verb is invoked.
    /// </summary>
    [DataField(required: true, serverOnly: true)]
    public AdminOperation[] Operations { get; private set; } = [];
}
