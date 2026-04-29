using Content.Shared.Temperature.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Kitchen.EntitySystems;

public partial class KitchenDeviceSystem
{
    /// <summary>
    /// Adds heat to all items in the container and their contained solutions.
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
