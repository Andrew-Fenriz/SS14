using Content.Shared.Administration.Verbs.Operations;
using Content.Shared.Administration.Verbs.Operations.Smites;
using Content.Shared.Silicons.Laws.Components;

namespace Content.Server.Administration.Systems.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnSiliconLawBound(Entity<SiliconLawProviderComponent> entity, ref AdminOperationEvent<SiliconLawBoundOperation> args)
    {
        EnsureComp<SiliconLawBoundComponent>(entity);

        // The provider is configured by an earlier operation
        // resolve its runtime lawset before notifying the target.
        _siliconLaws.GetLaws(entity.Owner);
        _siliconLaws.NotifyLawsChanged(entity);
    }
}
