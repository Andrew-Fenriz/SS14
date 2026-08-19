using Content.Shared.Administration.Smites;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Nutrition.Components;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminSmiteSystem
{
    [SubscribeLocalEvent]
    private void OnCreamPie(Entity<CreamPiedComponent> entity, ref SmiteOperationEvent<CreamPieSmite> args)
    {
        _creamPie.SetCreamPied(entity.AsNullable(), true);
    }

    [SubscribeLocalEvent]
    private void OnRemoveOrgans(Entity<BodyComponent> entity,
        ref SmiteOperationEvent<RemoveOrgansSmite> args)
    {
        if (args.Operation.MaxCount is <= 0)
            return;

        var selected = new List<EntityUid>();
        foreach (var organ in _body.EnumerateOrgans<TransformComponent>(entity.AsNullable()))
        {
            var category = organ.Comp1.Category;
            if (args.Operation.Categories != null &&
                (category == null || !args.Operation.Categories.Contains(category.Value)))
            {
                continue;
            }

            if (category != null && args.Operation.ExcludedCategories.Contains(category.Value))
                continue;

            selected.Add(organ);
            if (selected.Count == args.Operation.MaxCount)
                break;
        }

        foreach (var organ in selected)
        {
            if (args.Operation.Delete)
                QueueDel(organ);
            else
                _transform.AttachToGridOrMap(organ);
        }
    }

    [SubscribeLocalEvent]
    private void OnSpillBloodstream(Entity<BloodstreamComponent> entity,
        ref SmiteOperationEvent<SpillBloodstreamSmite> args)
    {
        _bloodstream.SpillAllSolutions(entity.AsNullable());
    }
}
