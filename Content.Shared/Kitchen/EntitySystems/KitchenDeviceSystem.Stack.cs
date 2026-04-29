using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Stacks;

namespace Content.Shared.Kitchen.EntitySystems;

public partial class KitchenDeviceSystem
{
    /// <summary>
    /// Processes stackable items, scaling the solution output based on how many fit in the beaker.
    /// </summary>
    public bool ProcessStackWithScaling(EntityUid item, Solution baseSolution, FixedPoint2 availableVolume,
        out Solution? processedSolution, out int itemsProcessed)
    {
        processedSolution = null;
        itemsProcessed = 0;

        if (!TryComp<StackComponent>(item, out var stack))
            return false;

        var totalVolume = baseSolution.Volume * stack.Count;
        if (totalVolume <= 0)
            return false;

        var fitsCount = (int)(stack.Count * FixedPoint2.Min(availableVolume / totalVolume + 0.01, 1));
        if (fitsCount <= 0)
            return false;

        var scaledSolution = new Solution(baseSolution);
        scaledSolution.ScaleSolution(fitsCount);
        processedSolution = scaledSolution;
        itemsProcessed = fitsCount;

        _stack.SetCount((item, stack), stack.Count - fitsCount);

        return true;
    }
}
