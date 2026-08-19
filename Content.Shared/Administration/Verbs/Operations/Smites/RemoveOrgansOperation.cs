using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Verbs.Operations.Smites;

/// <summary>
/// Removes matching organs from a body, either detaching them into the world or queueing them for deletion.
/// </summary>
public sealed partial class RemoveOrgansOperation : AdminOperationBase<RemoveOrgansOperation>
{
    /// <summary>
    /// Categories eligible for removal. Null allows organs of any category.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<OrganCategoryPrototype>>? Categories { get; private set; }

    /// <summary>
    /// Categories that are never eligible for removal. Exclusions take precedence over <see cref="Categories"/>.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<OrganCategoryPrototype>> ExcludedCategories { get; private set; } = [];

    /// <summary>
    /// Whether matching selected organs are queued for deletion instead of detached into the world.
    /// </summary>
    [DataField]
    public bool Delete { get; private set; }

    /// <summary>
    /// Maximum number of matching organs to remove.
    /// <see langword="null"/> removes every match; values less than or equal to zero remove nothing.
    /// </summary>
    [DataField]
    public int? MaxCount { get; private set; }
}
