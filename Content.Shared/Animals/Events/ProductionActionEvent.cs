using Content.Shared.Actions;

namespace Content.Shared.Animals.Events;

/// <summary>
/// Action event used to request production.
/// </summary>
public sealed partial class ProductionActionEvent : InstantActionEvent
{
    /// <summary>
    /// Entity containing the production configuration.
    /// </summary>
    [DataField]
    public ProductionActionSource Source = ProductionActionSource.Performer;
}

/// <summary>
/// Selects the entity containing the production configuration for an action.
/// </summary>
public enum ProductionActionSource : byte
{
    /// <summary>
    /// Production components are on the action performer.
    /// </summary>
    Performer,

    /// <summary>
    /// Production components are on the action entity.
    /// </summary>
    Action
}
