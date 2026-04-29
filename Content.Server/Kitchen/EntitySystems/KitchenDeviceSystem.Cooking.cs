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
    #region Ingredient Collection

    /// <summary>
    /// Collect ingredients from a container.
    /// Returns a CookingIngredients struct with solids, materials, and reagents.
    /// </summary>
    private CookingIngredients CollectIngredients(Container container)
    {
        var ingredients = new CookingIngredients();

        foreach (var item in container.ContainedEntities)
        {
            CollectIngredientsFromItem(item, ref ingredients);
        }

        return ingredients;
    }

    /// <summary>
    /// Collect ingredients from a single item.
    /// </summary>
    private void CollectIngredientsFromItem(EntityUid item, ref CookingIngredients ingredients)
    {
        // Solids
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

        // Reagents
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

    #endregion

    #region Ingredient Spending

    /// <summary>
    ///     Given a dictionary of materials that need to be spent in a recipe, and the amount of stacks
    ///     of a material we have available, this function gets the number of stacks we need to remove
    ///     from the stack entity. It also removes this amount from the remaining materials dictionary.
    /// </summary>
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

    /// <summary>
    ///     Given a dictionary of reagents that need to be spent in a recipe, and a quantity of a reagent
    ///     that we have available in a solution, this function gets the amount of reagents we need to
    ///     remove from the solution. This also removes that amount from the "reagents to spend" dictionary.
    /// </summary>
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

    /// <summary>
    ///     Removes a solid ingredient that is used in a recipe, removing it from the dictionary of
    ///     remaining solids that still need to be spent in the recipe.
    /// </summary>
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

    /// <summary>
    ///     Given a dictionary of remaining material stacks that need to be spent in a recipe, this function
    ///     reduces a stack entity's count by however many stacks need to be spent. This also removes the
    ///     material stack count from our remaining ingredients.
    /// </summary>
    private void SubtractMaterialContents(Entity<StackComponent> ent,
        ref CookingIngredients ingredientsToSpend)
    {
        var stack = ent.Comp;
        var stackId = stack.StackTypeId;
        var startingQuantity = stack.Count;
        var quantityToRemove = SpendMaterialQuantity(startingQuantity, stackId, ref ingredientsToSpend);

        _stack.ReduceCount(ent.AsNullable(), quantityToRemove);
    }

    /// <summary>
    ///     Given a dictionary of remaining reagents that still need to be spent in a recipe, this function iterates
    ///     over a solution's contents and subtracts reagents according to the reagents to spend. This also removes
    ///     it from our remaining ingredients.
    /// </summary>
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

    #endregion

    #region Ingredient Helpers

    /// <summary>
    ///     Attempt to get the solid ID of a given entity.
    /// </summary>
    /// <param name="item">The entity to retrieve a solid ID for.</param>
    /// <param name="solidId">The solid ID of the entity, if any.</param>
    /// <returns>
    ///     Whether or not the solid ID was successfully retrieved. False if entity lacks an entity prototype ID.
    /// </returns>
    /// <remarks>
    ///     TODO: Solids should be tag-based, or something like that. Not prototype-based.
    /// </remarks>
    private bool TryGetSolidId(EntityUid item,
        [NotNullWhen(true)] out EntProtoId? solidId)
    {
        solidId = MetaData(item).EntityPrototype?.ID;
        return solidId != null;
    }

    /// <summary>
    ///     Attempt to get the material stack ID of a given entity.
    /// </summary>
    /// <param name="item">The entity to retrieve a stack ID for</param>
    /// <param name="material">The stack prototype associated with this entity, if any.</param>
    /// <param name="stackEnt">This entity represented as an entity with StackComponent, if feasible.</param>
    /// <returns>
    ///     Whether or not a material ID is successfully retrieved. False if this entity is not a stack.
    /// </returns>
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

    /// <summary>
    ///     Attempts to get a solution from an entity that can be used as viable ingredients in a recipe.
    /// </summary>
    /// <remarks>
    ///     For example, a beaker's contents will work, but not the contents of an uncracked egg.
    /// </remarks>
    /// <param name="uid">The entity to attempt to get a usable ingredient solution for.</param>
    /// <param name="solutionEntity">A usable solution entity, if available.</param>
    /// <param name="solution">A usable solution, if available.</param>
    /// <returns>Whether or not a usable ingredient solution was successfully retrieved.</returns>
    private bool TryGetUsableIngredientSolution(EntityUid uid,
        [NotNullWhen(true)] out Entity<SolutionComponent>? solutionEntity,
        [NotNullWhen(true)] out Solution? solution)
    {
        return _solutionContainer.TryGetDrainableSolution(uid, out solutionEntity, out solution);
    }

    #endregion
}
