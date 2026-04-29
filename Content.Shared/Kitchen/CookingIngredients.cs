using System.Linq;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Shared.Kitchen;

/// <summary>
/// A data value representing ingredients for an appliance recipe.
/// </summary>
[Serializable, DataDefinition]
public partial record struct CookingIngredients
{
    public CookingIngredients(Dictionary<EntProtoId, int> solids,
        Dictionary<ProtoId<StackPrototype>, int> materials,
        Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> reagents)
    {
        Solids = solids;
        Materials = materials;
        Reagents = reagents;
    }

    /// <summary>
    /// A dictionary of solid item ingredient quantities - actual items used in a recipe.
    /// </summary>
    // TODO: This should use tags or whitelists instead of entity prototype IDs
    [DataField]
    public Dictionary<EntProtoId, int> Solids { get; private set; } = new();

    /// <summary>
    /// A dictionary of stack material quantities, such as plastic sheets or cloth rolls.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<StackPrototype>, int> Materials { get; private set; } = new();

    /// <summary>
    /// A dictionary of reagent quantities.
    /// </summary>
    [DataField]
    // TODO: Use ReagentQuantity[]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> Reagents { get; private set; } = new();

    /// <summary>
    ///     Adds a quantity of a solid ingredient to this ingredients list.
    /// </summary>
    /// <param name="solidId">The ID of the solid to add.</param>
    /// <param name="count">How much of the solid to add. 1 by default. Must be non-negative.</param>
    public readonly void AddSolid(EntProtoId solidId, int count = 1)
    {
        if (count < 0)
            throw new ArgumentException("Count cannot be negative.", nameof(count));
        if (count == 0)
            return;

        var newCount = Solids.GetValueOrDefault(solidId) + count;
        if (newCount > 0)
            Solids[solidId] = newCount;
        else
            Solids.Remove(solidId);
    }

    /// <summary>
    ///     Adds a quantity of a material stack to this ingredients list.
    /// </summary>
    /// <param name="materialId">The ID of the material stack to add.</param>
    /// <param name="count">How many stacks to add. Must be non-negative.</param>
    public readonly void AddMaterial(ProtoId<StackPrototype> materialId, int count)
    {
        if (count < 0)
            throw new ArgumentException("Count cannot be negative.", nameof(count));
        if (count == 0)
            return;

        var newCount = Materials.GetValueOrDefault(materialId) + count;
        if (newCount > 0)
            Materials[materialId] = newCount;
        else
            Materials.Remove(materialId);
    }

    /// <summary>
    ///     Adds a quantity of a reagent to this ingredients list.
    /// </summary>
    /// <param name="reagentId">The ID of the reagent to add.</param>
    /// <param name="quantity">The volume of the reagent to add. Must be non-negative.</param>
    public readonly void AddReagent(ProtoId<ReagentPrototype> reagentId, FixedPoint2 quantity)
    {
        if (quantity < 0)
            throw new ArgumentException("Quantity cannot be negative.", nameof(quantity));
        if (quantity == 0)
            return;

        var newQuantity = Reagents.GetValueOrDefault(reagentId) + quantity;
        if (newQuantity > 0)
            Reagents[reagentId] = newQuantity;
        else
            Reagents.Remove(reagentId);
    }

    /// <summary>
    ///    Count the number of ingredients in a recipe for sorting the recipe list.
    ///    This makes sure that where ingredient lists overlap, the more complex
    ///    recipe is picked first.
    /// </summary>
    public readonly FixedPoint2 Count()
    {
        var solidCount = Solids.Sum(s => s.Value);
        var reagentCount = Reagents.Count;
        var materialCount = Materials.Sum(s => s.Value);

        return solidCount + reagentCount + materialCount;
    }

    /// <summary>
    ///     Get the number of times a given recipe can be made with this struct's ingredients.
    /// </summary>
    /// <param name="recipe">The recipe to attempt to make with these ingredients.</param>
    /// <returns>How many times the given recipe can be made.</returns>
    public readonly uint PortionForRecipe(CookingIngredients recipe)
    {
        var solidPortions = GetTimesFulfilled(Solids, recipe.Solids,
            (available, count) => (uint)(available / count));
        if (solidPortions == 0)
            return 0;

        var materialPortions = GetTimesFulfilled(Materials, recipe.Materials,
            (available, count) => (uint)(available / count));
        if (materialPortions == 0)
            return 0;

        var reagentPortions = GetTimesFulfilled(Reagents, recipe.Reagents,
            (available, count) => (uint)(available / count).Int());
        if (reagentPortions == 0)
            return 0;

        return new[] { solidPortions, materialPortions, reagentPortions }.Min();
    }

    /// <summary>
    ///     Given an ingredient dictionary, and a recipe's ingredient dictionary, gets the maximum
    ///     amount of times the recipe can be fulfilled with our available ingredients.
    /// </summary>
    /// <typeparam name="T">The key of the ingredient dictionary.</typeparam>
    /// <typeparam name="TCount">A numerical quantity of the ingredient in the dictionary.</typeparam>
    /// <param name="ingredients">A dictionary of available ingredients.</param>
    /// <param name="recipe">A recipe's dictionary of required ingredients.</param>
    /// <param name="divide">Function to divide the recipe's ingredient count by our ingredient count.</param>
    /// <returns>How many times the given recipe's ingredients can be fulfilled.</returns>
    private static uint GetTimesFulfilled<T, TCount>(Dictionary<T, TCount> ingredients,
        Dictionary<T, TCount> recipe,
        Func<TCount, TCount, uint> divide)
        where T : notnull
    {
        var portions = uint.MaxValue;

        foreach (var (ingredient, count) in recipe)
        {
            if (!ingredients.TryGetValue(ingredient, out var available))
                return 0;

            var ingredientPortions = divide(available, count);
            portions = Math.Min(portions, ingredientPortions);
        }

        return portions;
    }

    /// <summary>
    /// Multiplies all ingredient quantities by a factor.
    /// </summary>
    public static CookingIngredients operator *(CookingIngredients c1, int scalar)
    {
        var scaledSolids = c1.Solids.ToDictionary(kvp => kvp.Key,
            kvp => kvp.Value * scalar);
        var scaledMaterials = c1.Materials.ToDictionary(kvp => kvp.Key,
            kvp => kvp.Value * scalar);
        var scaledReagents = c1.Reagents.ToDictionary(kvp => kvp.Key,
            kvp => kvp.Value * scalar);

        return new CookingIngredients(scaledSolids, scaledMaterials, scaledReagents);
    }

    public static CookingIngredients operator *(CookingIngredients c1, uint scalar)
    {
        return c1 * (int)scalar;
    }

    public static CookingIngredients operator +(CookingIngredients c1, CookingIngredients c2)
    {
        var result = c1;
        foreach (var (key, count) in c2.Solids)
        {
            result.AddSolid(key, count);
        }

        foreach (var (key, count) in c2.Materials)
        {
            result.AddMaterial(key, count);
        }

        foreach (var (key, quantity) in c2.Reagents)
        {
            result.AddReagent(key, quantity);
        }

        return result;
    }
}
