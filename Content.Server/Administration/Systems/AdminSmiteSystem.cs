using Content.Server.GhostKick;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Administration.Prototypes;
using Content.Shared.Administration.Smites;
using Content.Shared.Body;
using Content.Shared.Body.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Friction;
using Content.Shared.Inventory;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Stunnable;
using Content.Shared.Tools.Systems;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Server.Administration.Systems;

/// <summary>
/// Executes the ordered operations belonging to declarative admin smites.
/// </summary>
public sealed partial class AdminSmiteSystem : EntitySystem, ISmiteOperationRaiser
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private SharedCreamPieSystem _creamPie = default!;
    [Dependency] private SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private EntityStorageSystem _entityStorage = default!;
    [Dependency] private FixtureSystem _fixtures = default!;
    [Dependency] private GhostKickManager _ghostKick = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private PolymorphSystem _polymorph = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TileFrictionController _tileFriction = default!;
    [Dependency] private WeldableSystem _weldable = default!;

    public void Apply(EntityUid target, EntityUid user, AdminSmitePrototype prototype)
    {
        Apply(target, user, prototype.Operations);
    }

    public void Apply(EntityUid target, EntityUid user, SmiteOperation[] operations)
    {
        foreach (var operation in operations)
        {
            operation.RaiseEvent(target, user, this);
        }
    }

    public void RaiseOperationEvent<T>(EntityUid target, EntityUid user, T operation)
        where T : SmiteOperationBase<T>
    {
        var operationEvent = new SmiteOperationEvent<T>(operation, user);
        RaiseLocalEvent(target, ref operationEvent);
    }
}
