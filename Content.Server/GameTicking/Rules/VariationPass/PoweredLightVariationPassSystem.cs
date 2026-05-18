using Content.Server.GameTicking.Rules.VariationPass.Components;
using Content.Server.Light.EntitySystems;
using Content.Shared.Light.Components;
using Content.Shared.Light.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules.VariationPass;

/// <inheritdoc cref="PoweredLightVariationPassComponent"/>
public sealed partial class PoweredLightVariationPassSystem : VariationPassSystem<PoweredLightVariationPassComponent>
{
    [Dependency] private PoweredLightSystem _poweredLight = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    protected override void ApplyVariation(Entity<PoweredLightVariationPassComponent> ent, ref StationVariationPassEvent args)
    {
        var query = AllEntityQuery<PoweredLightComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (!IsMemberOfStation((uid, xform), ref args))
                continue;

            if (Random.Prob(ent.Comp.LightBreakChance))
            {
                if (TryGetBulbType(comp, out var bulbType) && bulbType?.BrokenPrototype is { } broken)
                    _poweredLight.ReplaceSpawnedPrototype((uid, comp), broken);

                continue;
            }

            if (!Random.Prob(ent.Comp.LightAgingChance))
                continue;

            if (!TryGetBulbType(comp, out var agedBulbType))
                continue;

            // some aging light bulbs start to flicker
            // its also way too annoying right now so we wrap it in another prob lol
            if (agedBulbType is { FlickersWhenAged: true } && Random.Prob(ent.Comp.AgedLightTubeFlickerChance))
                EnsureComp<BlinkingPoweredLightComponent>(uid);

            if (agedBulbType?.AgedPrototype is { } aged)
                _poweredLight.ReplaceSpawnedPrototype((uid, comp), aged);
        }
    }

    private bool TryGetBulbType(PoweredLightComponent light, out LightBulbTypePrototype? prototype)
    {
        if (_prototype.TryIndex(light.BulbType, out var indexed))
        {
            prototype = indexed;
            return true;
        }

        prototype = default;
        return false;
    }
}
