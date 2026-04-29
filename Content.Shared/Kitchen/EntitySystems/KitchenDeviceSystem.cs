using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Temperature.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.Kitchen.EntitySystems;

[Virtual]
public partial class KitchenDeviceSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedTemperatureSystem _temperature = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
}
