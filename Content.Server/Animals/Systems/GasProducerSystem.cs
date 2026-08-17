using Content.Server.Animals.Components;
using Content.Server.Atmos.EntitySystems;

namespace Content.Server.Animals.Systems;

/// <inheritdoc cref="GasProducerComponent"/>
public sealed partial class GasProducerSystem : EntitySystem
{
    [Dependency] private AtmosphereSystem _atmos = default!;

    [SubscribeLocalEvent]
    private void OnProduce(Entity<GasProducerComponent> ent, ref ProductionAttemptEvent args)
    {
        var mixture = _atmos.GetTileMixture(args.Producer, excite: true);
        var produced = false;

        foreach (var (gas, moles) in ent.Comp.Gases)
        {
            if (moles <= 0f)
                continue;

            produced = true;
            mixture?.AdjustMoles(gas, moles);
        }

        if (produced)
            args.Produced = true;
    }
}
