using Content.Shared.Administration.Verbs.Operations;
using Content.Shared.Administration.Verbs.Operations.Smites;
using Content.Shared.Silicons.Laws.Components;

namespace Content.Server.Administration.Systems.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnSiliconLawBound(Entity<MetaDataComponent> entity,
        ref AdminOperationEvent<SiliconLawBoundOperation> args)
    {
        EnsureComp<SiliconLawBoundComponent>(entity);

        _siliconLaws.GetLaws(entity.Owner);
        var provider = Comp<SiliconLawProviderComponent>(entity);
        _siliconLaws.NotifyLawsChanged((entity.Owner, provider));
    }
}
