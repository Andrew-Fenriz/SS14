using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Verbs.Operations.Smites;

/// <summary>
/// Removes matching organs from a body and leaves them in the world or deletes them.
/// </summary>
public sealed partial class RemoveOrgansOperation : AdminOperationBase<RemoveOrgansOperation>
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
    /// Whether matching selected organs are queued for deletion instead of detached into the world.
    /// </summary>
    [DataField]
    public bool Delete { get; private set; }

    /// <summary>
    /// Maximum number of matching organs to remove. Null removes every match.
    /// </summary>
    [DataField]
    public int? MaxCount { get; private set; }
}
