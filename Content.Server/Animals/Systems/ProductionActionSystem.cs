using Content.Server.Animals.Components;
using Content.Shared.Animals.Events;

namespace Content.Server.Animals.Systems;

/// <summary>
/// Handles actions that request production.
/// </summary>
public sealed partial class ProductionActionSystem : EntitySystem
{
    [Dependency] private ProductionSystem _production = default!;

    [SubscribeLocalEvent]
    private void OnProductionAction(Entity<ProductionActionComponent> ent, ref ProductionActionEvent args)
    {
        if (args.Handled)
            return;

        var source = args.Source switch
        {
            ProductionActionSource.Action => args.Action.Owner,
            _ => args.Performer
        };

        args.Handled = _production.TryProduce(
            source,
            args.Performer,
            args.Performer);
    }
}
