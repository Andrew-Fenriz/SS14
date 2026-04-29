using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Kitchen;
using Content.Shared.Stacks;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Kitchen.EntitySystems;

public partial class KitchenDeviceSystem
{
    private CookingIngredients CollectIngredients(Container container)
    {
        var ingredients = new CookingIngredients();

        foreach (var item in container.ContainedEntities)
        {
            CollectIngredientsFromItem(item, ref ingredients);
        }

        return ingredients;
    }

    private void CollectIngredientsFromItem(EntityUid item, ref CookingIngredients ingredients)
    {
        if (TryComp<StackComponent>(item, out var stackComp))
        {
            var materialId = stackComp.StackTypeId;
            ingredients.AddMaterial(materialId, stackComp.Count);
        }
        else
        {
            var metaData = MetaData(item);
            if (metaData.EntityPrototype is not null)
                ingredients.AddSolid(metaData.EntityPrototype.ID);
        }

        if (!TryComp(item, out SolutionManagerComponent? solutionContainer)) return;
        foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((item, solutionContainer)))
        {
            var solution = soln.Comp.Solution;
            foreach (var (reagent, quantity) in solution.Contents)
            {
                ingredients.AddReagent(reagent.Prototype, quantity);
            }
        }
    }

    private static int SpendMaterialQuantity(int availableStacks,
        ProtoId<StackPrototype> stackId,
        ref CookingIngredients ingredientsToSpend)
    {
        if (!ingredientsToSpend.Materials.TryGetValue(stackId, out var remaining))
            return 0;

        var spent = Math.Min(availableStacks, remaining);
        ingredientsToSpend.Materials[stackId] -= spent;

        return spent;
    }

    private static FixedPoint2 SpendReagentQuantity(FixedPoint2 availableQuantity,
        ProtoId<ReagentPrototype> reagent,
        ref CookingIngredients ingredientsToSpend)
    {
        if (!ingredientsToSpend.Reagents.TryGetValue(reagent, out var remaining))
            return 0;

        var spent = FixedPoint2.Min(availableQuantity, remaining);
        ingredientsToSpend.Reagents[reagent] -= spent;

        return spent;
    }

    private void SubtractSolidContents(EntityUid item,
        EntProtoId itemProto,
        Container container,
        ref CookingIngredients ingredientsToSpend)
    {
        if (!ingredientsToSpend.Solids.ContainsKey(itemProto))
            return;

        ingredientsToSpend.Solids[itemProto] -= 1;
        EjectEntity(item, container);
        QueueDel(item);
    }

    private void SubtractMaterialContents(Entity<StackComponent> ent,
        ref CookingIngredients ingredientsToSpend)
    {
        var stack = ent.Comp;
        var stackId = stack.StackTypeId;
        var startingQuantity = stack.Count;
        var quantityToRemove = SpendMaterialQuantity(startingQuantity, stackId, ref ingredientsToSpend);

        _stack.SetCount((ent.Owner, ent.Comp), startingQuantity - quantityToRemove);
    }

    private void SubtractReagentContents(Entity<SolutionComponent> solutionEntity,
        Solution solution,
        ref CookingIngredients ingredientsToSpend)
    {
        var reagentsToProcess = ingredientsToSpend.Reagents.Keys.ToList();

        foreach (var reagent in reagentsToProcess)
        {
            var availableQuantity = solution.GetTotalPrototypeQuantity(reagent);
            if (availableQuantity == 0)
                continue;

            var quantityToRemove = SpendReagentQuantity(availableQuantity, reagent, ref ingredientsToSpend);
            _solutionContainer.RemoveReagent(solutionEntity, reagent, quantityToRemove);
        }
    }

    // Attempts to get a solid prototype id from an entity.
    // TODO: Solids should be tag-based, or something like that. Not prototype-based.
    private bool TryGetSolidId(EntityUid item,
        [NotNullWhen(true)] out EntProtoId? solidId)
    {
        solidId = MetaData(item).EntityPrototype?.ID;
        return solidId != null;
    }

    private bool TryGetMaterialId(EntityUid item,
        [NotNullWhen(true)] out ProtoId<StackPrototype>? material,
        [NotNullWhen(true)] out Entity<StackComponent>? stackEnt)
    {
        material = null;
        stackEnt = null;

        if (!TryComp<StackComponent>(item, out var stack))
            return false;

        material = stack.StackTypeId;
        stackEnt = (item, stack);

        return true;
    }

    // Attempts to get a drainable ingredient solution from an entity.
    // For example, a beaker's contents will work, but not the contents of an uncracked egg.
    private bool TryGetUsableIngredientSolution(EntityUid uid,
        [NotNullWhen(true)] out Entity<SolutionComponent>? solutionEntity,
        [NotNullWhen(true)] out Solution? solution)
    {
        return _solutionContainer.TryGetDrainableSolution(uid, out solutionEntity, out solution);
    }
}
