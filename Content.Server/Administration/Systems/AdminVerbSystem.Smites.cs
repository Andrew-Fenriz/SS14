using Content.Server.Popups;
using Content.Server.Roles;
using Content.Server.Storage.EntitySystems;
using Content.Server.Tabletop;
using Content.Shared.Actions;
using Content.Shared.Administration;
using Content.Shared.Administration.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Slippery;
using Content.Shared.Storage.Components;
using Content.Shared.Tabletop.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityStorageSystem _entityStorageSystem = default!;
    [Dependency] private FixtureSystem _fixtures = default!;
    [Dependency] private SharedGodmodeSystem _sharedGodmodeSystem = default!;
    [Dependency] private InventorySystem _inventorySystem = default!;
    [Dependency] private PopupSystem _popupSystem = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private RoleSystem _role = default!;
    [Dependency] private TabletopSystem _tabletopSystem = default!;
    [Dependency] private WeldableSystem _weldableSystem = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;
    [Dependency] private SlipperySystem _slipperySystem = default!;
    [Dependency] private AdminSmiteSystem _smiteSystem = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;

    private readonly EntProtoId _actionViewLawsProtoId = "ActionViewLaws";
    private readonly ProtoId<SiliconLawsetPrototype> _crewsimovLawset = "Crewsimov";

    private readonly EntProtoId _siliconMindRole = "MindRoleSiliconBrain";
    private const string SiliconLawBoundUserInterface = "SiliconLawBoundUserInterface";

    // All smite verbs have names so invokeverb works.
    private void AddSmiteVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Fun))
            return;

        // 1984.
        if (HasComp<MapComponent>(args.Target) || HasComp<MapGridComponent>(args.Target))
            return;

        AddPrototypeSmiteVerbs(args);

        var chessName = Loc.GetString("admin-smite-chess-dimension-name").ToLowerInvariant();
        Verb chess = new()
        {
            Text = chessName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Objects/Fun/Tabletop/chessboard.rsi"), "chessboard"),
            Act = () =>
            {
                _sharedGodmodeSystem.EnableGodmode(args.Target); // So they don't suffocate.
                EnsureComp<TabletopDraggableComponent>(args.Target);
                var xform = Transform(args.Target);
                _popupSystem.PopupEntity(Loc.GetString("admin-smite-chess-self"), args.Target,
                    args.Target, PopupType.LargeCaution);
                _popupSystem.PopupCoordinates(
                    Loc.GetString("admin-smite-chess-others", ("name", args.Target)), xform.Coordinates,
                    Filter.PvsExcept(args.Target), true, PopupType.MediumCaution);
                var board = Spawn("ChessBoard", xform.Coordinates);
                var session = _tabletopSystem.EnsureSession(Comp<TabletopGameComponent>(board));
                _transformSystem.SetMapCoordinates(args.Target, session.Position);
                _transformSystem.SetWorldRotationNoLerp((args.Target, xform), Angle.Zero);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", chessName, Loc.GetString("admin-smite-chess-dimension-description"))
        };
        args.Verbs.Add(chess);

        if (TryComp<PhysicsComponent>(args.Target, out var physics))
        {
            var pinballName = Loc.GetString("admin-smite-pinball-name").ToLowerInvariant();
            Verb pinball = new()
            {
                Text = pinballName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new("/Textures/Objects/Fun/Balls/basketball.rsi"), "icon"),
                Act = () =>
                {
                    var xform = Transform(args.Target);
                    var fixtures = Comp<FixturesComponent>(args.Target);
                    _transformSystem.Unanchor(args.Target, xform); // Just in case.
                    _physics.SetBodyType(args.Target, BodyType.Dynamic, manager: fixtures, body: physics);
                    _physics.SetBodyStatus(args.Target, physics, BodyStatus.InAir);
                    _physics.WakeBody(args.Target, manager: fixtures, body: physics);

                    foreach (var fixture in fixtures.Fixtures.Values)
                    {
                        if (!fixture.Hard)
                            continue;

                        _physics.SetRestitution(args.Target, fixture, 1.1f, false, fixtures);
                    }

                    _fixtures.FixtureUpdate(args.Target, manager: fixtures, body: physics);

                    _physics.SetLinearVelocity(args.Target, _random.NextVector2(1.5f, 1.5f), manager: fixtures, body: physics);
                    _physics.SetAngularVelocity(args.Target, MathF.PI * 12, manager: fixtures, body: physics);
                    _physics.SetLinearDamping(args.Target, physics, 0f);
                    _physics.SetAngularDamping(args.Target, physics, 0f);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", pinballName, Loc.GetString("admin-smite-pinball-description"))
            };
            args.Verbs.Add(pinball);

            var yeetName = Loc.GetString("admin-smite-yeet-name").ToLowerInvariant();
            Verb yeet = new()
            {
                Text = yeetName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),
                Act = () =>
                {
                    var xform = Transform(args.Target);
                    var fixtures = Comp<FixturesComponent>(args.Target);
                    _transformSystem.Unanchor(args.Target); // Just in case.

                    _physics.SetBodyType(args.Target, BodyType.Dynamic, body: physics);
                    _physics.SetBodyStatus(args.Target, physics, BodyStatus.InAir);
                    _physics.WakeBody(args.Target, manager: fixtures, body: physics);

                    foreach (var fixture in fixtures.Fixtures.Values)
                    {
                        _physics.SetHard(args.Target, fixture, false, manager: fixtures);
                    }

                    _physics.SetLinearVelocity(args.Target, _random.NextVector2(8.0f, 8.0f), manager: fixtures, body: physics);
                    _physics.SetAngularVelocity(args.Target, MathF.PI * 12, manager: fixtures, body: physics);
                    _physics.SetLinearDamping(args.Target, physics, 0f);
                    _physics.SetAngularDamping(args.Target, physics, 0f);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", yeetName, Loc.GetString("admin-smite-yeet-description"))
            };
            args.Verbs.Add(yeet);
        }

        var lockerName = Loc.GetString("admin-smite-locker-stuff-name").ToLowerInvariant();
        Verb locker = new()
        {
            Text = lockerName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Structures/Storage/closet.rsi"), "generic"),
            Act = () =>
            {
                var xform = Transform(args.Target);
                var locker = Spawn("ClosetMaintenance", xform.Coordinates);
                if (TryComp<EntityStorageComponent>(locker, out var storage))
                {
                    _entityStorageSystem.ToggleOpen(args.Target, locker, storage);
                    _entityStorageSystem.Insert(args.Target, locker, storage);
                    _entityStorageSystem.ToggleOpen(args.Target, locker, storage);
                }
                _weldableSystem.SetWeldedState(locker, true);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", lockerName, Loc.GetString("admin-smite-locker-stuff-description"))
        };
        args.Verbs.Add(locker);

        var superslipName = Loc.GetString("admin-smite-super-slip-name").ToLowerInvariant();
        Verb superslip = new()
        {
            Text = superslipName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new("Objects/Specific/Janitorial/soap.rsi"), "omega-4"),
            Act = () =>
            {
                var hadSlipComponent = EnsureComp(args.Target, out SlipperyComponent slipComponent);
                if (!hadSlipComponent)
                {
                    slipComponent.SlipData.SuperSlippery = true;
                    slipComponent.SlipData.StunTime = TimeSpan.FromSeconds(5);
                    slipComponent.SlipData.LaunchForwardsMultiplier = 20;
                }

                _slipperySystem.TrySlip(args.Target, slipComponent, args.Target, requiresContact: false);
                if (!hadSlipComponent)
                {
                    RemComp(args.Target, slipComponent);
                }
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", superslipName, Loc.GetString("admin-smite-super-slip-description"))
        };
        args.Verbs.Add(superslip);

        var siliconName = Loc.GetString("admin-smite-silicon-laws-bound-name").ToLowerInvariant();
        Verb silicon = new()
        {
            Text = siliconName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new("Interface/Actions/actions_borg.rsi"), "state-laws"),
            Act = () =>
            {
                var userInterfaceComp = EnsureComp<UserInterfaceComponent>(args.Target);
                _uiSystem.SetUi((args.Target, userInterfaceComp), SiliconLawsUiKey.Key, new InterfaceData(SiliconLawBoundUserInterface));

                if (!HasComp<SiliconLawBoundComponent>(args.Target))
                {
                    EnsureComp<SiliconLawBoundComponent>(args.Target);
                    _actions.AddAction(args.Target, _actionViewLawsProtoId);
                }

                EnsureComp<SiliconLawProviderComponent>(args.Target);
                _siliconLawSystem.SetLaws(_siliconLawSystem.GetLawset(_crewsimovLawset).Laws, args.Target);

                if (_mindSystem.TryGetMind(args.Target, out var mindId, out _))
                    _role.MindAddRole(mindId, _siliconMindRole);

                _popupSystem.PopupEntity(Loc.GetString("admin-smite-silicon-laws-bound-self"), args.Target,
                    args.Target, PopupType.LargeCaution);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", siliconName, Loc.GetString("admin-smite-silicon-laws-bound-description"))
        };
        args.Verbs.Add(silicon);
    }

    private void AddPrototypeSmiteVerbs(GetVerbsEvent<Verb> args)
    {
        foreach (var prototype in ProtoMan.EnumeratePrototypes<AdminSmitePrototype>())
        {
            if (!_whitelistSystem.CheckBoth(args.Target, whitelist: prototype.Whitelist))
                continue;

            var name = Loc.GetString(prototype.Name).ToLowerInvariant();
            var verb = new Verb
            {
                Text = name,
                Category = VerbCategory.Smite,
                Icon = prototype.Icon,
                Act = () => _smiteSystem.Apply(args.Target, args.User, prototype),
                Impact = LogImpact.Extreme,
                Message = prototype.Description is { } description
                    ? string.Join(": ", name, Loc.GetString(description))
                    : null
            };
            args.Verbs.Add(verb);
        }
    }
}
