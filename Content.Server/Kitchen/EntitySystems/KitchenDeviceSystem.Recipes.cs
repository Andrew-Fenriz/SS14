using System.Linq;
using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.Server.Kitchen.EntitySystems;

public sealed partial class KitchenDeviceSystem
{
    /// <summary>
    /// Stores the currently selected recipe in the active component.
    /// </summary>
    public void SetRecipeData(EntityUid uid, string? recipeId, int portionCount)
    {
        if (!TryComp<ActiveKitchenDeviceComponent>(uid, out var activeComp))
            return;

        activeComp.RecipeId = recipeId;
        activeComp.PortionCount = portionCount;
        Dirty(uid, activeComp);
    }

    private (FoodRecipePrototype?, int) FindBestRecipe(
        Container container,
        IReadOnlyList<FoodRecipePrototype> recipes,
        uint cookTime)
    {
        var ingredients = CollectIngredients(container);

        var bestRecipe = recipes
            .Select(r =>
            {
                var portions = (int)ingredients.PortionForRecipe(r.Ingredients);
                if (cookTime % r.CookTime != 0 || portions == 0)
                    return (r, 0);
                return (r, (int)Math.Min(portions, cookTime / r.CookTime));
            })
            .FirstOrDefault(r => r.Item2 > 0);

        return bestRecipe;
    }

    /// <summary>
    /// Collects secret recipes from nearby recipe providers (e.g., cookbooks).
    /// </summary>
    public void CollectSecretRecipes(Entity<FoodRecipeProviderComponent> provider, ref GetSecretRecipesEvent args)
    {
        foreach (var recipeId in provider.Comp.ProvidedRecipes)
        {
            if (_prototype.TryIndex(recipeId, out var recipeProto))
            {
                args.Recipes.Add(recipeProto);
            }
        }
    }

    /// <summary>
    /// Removes ingredients from the container based on the recipe requirements.
    /// </summary>
    public void SubtractRecipeIngredients(Container container, string recipeId, uint count = 1)
    {
        if (!_prototype.TryIndex<FoodRecipePrototype>(recipeId, out var recipe)) return;

        SubtractRecipeIngredients(container, recipe, count);
    }

    private void SubtractRecipeIngredients(Container container, FoodRecipePrototype recipe, uint count = 1)
    {
        var ingredientsToSpend = recipe.Ingredients * count;
        var solidsToSpend = ingredientsToSpend.Solids;
        var materialsToSpend = ingredientsToSpend.Materials;
        var reagentsToSpend = ingredientsToSpend.Reagents;
        var items = container.ContainedEntities.ToArray();

        foreach (var item in items)
        {
            if (solidsToSpend.Count > 0
                && TryGetSolidId(item, out var solidId)
                && solidsToSpend.ContainsKey(solidId.Value))
            {
                SubtractSolidContents(item, solidId.Value, container, ref ingredientsToSpend);
                continue;
            }

            if (materialsToSpend.Count > 0
                && TryGetMaterialId(item, out var materialId, out var stack)
                && materialsToSpend.ContainsKey(materialId.Value))
            {
                SubtractMaterialContents(stack.Value, ref ingredientsToSpend);
                if (Deleted(stack.Value.Owner) || stack.Value.Comp.Count <= 0)
                    continue;
            }

            if (reagentsToSpend.Count > 0
                && TryGetUsableIngredientSolution(item, out var solutionEntity, out var solution)
                && solution.Volume > 0)
                SubtractReagentContents(solutionEntity.Value, solution, ref ingredientsToSpend);
        }
    }

    private List<FoodRecipePrototype> GetRecipesForDevice(EntityUid uid)
    {
        var recipes = new List<FoodRecipePrototype>(_recipeManager.Recipes);

        var getSecretRecipesEv = new GetSecretRecipesEvent();
        RaiseLocalEvent(uid, ref getSecretRecipesEv);
        recipes.AddRange(getSecretRecipesEv.Recipes);

        return recipes;
    }

    /// <summary>
    /// Finds the best matching recipe for the current container contents and cook time.
    /// </summary>
    public (FoodRecipePrototype? recipe, int count) FindBestRecipeForDevice(EntityUid uid, Container container, uint cookTime)
    {
        var recipes = GetRecipesForDevice(uid);
        return FindBestRecipe(container, recipes, cookTime);
    }

    /// <summary>
    /// Spawns the recipe results at the specified coordinates.
    /// </summary>
    public void SpawnRecipeResults(string? recipeId, int portionCount, EntityCoordinates coords)
    {
        if (recipeId == null || !_prototype.TryIndex<FoodRecipePrototype>(recipeId, out var recipe)) return;

        for (var i = 0; i < portionCount; i++)
        {
            Spawn(recipe.Result, coords);
        }
    }
}
