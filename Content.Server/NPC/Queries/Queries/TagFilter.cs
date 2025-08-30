namespace Content.Server.NPC.Queries.Queries;

public sealed partial class TagFilter : UtilityQueryFilter
{
    /// <summary>
    ///     Tags to filter for.
    /// </summary>
    [DataField(required: true)]
    public List<string> Tags = new();
}
