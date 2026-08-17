using Content.Server.Animals.Systems;

namespace Content.Server.Animals.Components;

/// <summary>
/// Allows an entity to handle production actions.
/// </summary>
[RegisterComponent, Access(typeof(ProductionActionSystem))]
public sealed partial class ProductionActionComponent : Component;
