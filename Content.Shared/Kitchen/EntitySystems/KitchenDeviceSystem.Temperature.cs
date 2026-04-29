using Content.Shared.Temperature.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Kitchen.EntitySystems;

public partial class KitchenDeviceSystem
{
    /// <summary>
    /// Adds temperature to every item in the container,
    /// based on the time it took to cook.
    /// </summary>
    public void AddTemperature(
        Container container,
        float time,
        float baseHeatMultiplier,
        float objectHeatMultiplier,
        float temperatureUpperThreshold = float.MaxValue)
    {
        var heatToAdd = time * baseHeatMultiplier;

        foreach (var entity in container.ContainedEntities)
        {
            if (TryComp<TemperatureComponent>(entity, out var tempComp))
                _temperature.ChangeHeat(entity, heatToAdd * objectHeatMultiplier, false, tempComp);

            foreach (var (_, soln) in _solutionContainer.EnumerateSolutions(entity))
            {
                var solution = soln.Comp.Solution;
                if (solution.Temperature > temperatureUpperThreshold)
                    continue;

                _solutionContainer.AddThermalEnergy(soln, heatToAdd);
            }
        }
    }
}
