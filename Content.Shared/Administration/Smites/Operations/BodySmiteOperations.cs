using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Smites.Operations;

/// <summary>
/// Removes matching organs from a body and leaves them in the world.
/// </summary>
public sealed partial class RemoveOrgansSmite : SmiteOperationBase<RemoveOrgansSmite>
{
    /// <summary>
    /// Categories eligible for removal. Null allows organs of any category.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<OrganCategoryPrototype>>? Categories { get; private set; }

    /// <summary>
    /// Categories that are never eligible for removal.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<OrganCategoryPrototype>> ExcludedCategories { get; private set; } = [];

    /// <summary>
    /// Maximum number of matching organs to remove. Null removes every match.
    /// </summary>
    [DataField]
    public int? MaxCount { get; private set; }
}

/// <summary>
/// Spills every solution in the smite target's bloodstream.
/// </summary>
public sealed partial class SpillBloodstreamSmite : SmiteOperationBase<SpillBloodstreamSmite>;
