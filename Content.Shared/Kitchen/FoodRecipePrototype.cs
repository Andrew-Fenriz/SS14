using Robust.Shared.Prototypes;

namespace Content.Shared.Kitchen;

/// <summary>
///    A recipe for space microwaves.
/// </summary>
[Prototype]
public sealed partial class FoodRecipePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     The guidebook grouping for this recipe.
    /// </summary>
    [DataField]
    public string Group = "Other";

    /// <summary>
    ///     The cooking ingredients used in this recipe.
    /// </summary>
    [DataField(required: true)]
    public CookingIngredients Ingredients;

    /// <summary>
    ///     The resulting entity made from this recipe.
    /// </summary>
    [DataField]
    public EntProtoId Result { get; private set; } = string.Empty;

    /// <summary>
    ///     The cooking time of this recipe.
    /// </summary>
    [DataField("time")]
    public uint CookTime { get; private set; } = 5;

    /// <summary>
    /// Is this recipe unavailable in normal circumstances?
    /// </summary>
    [DataField]
    public bool SecretRecipe;
}
