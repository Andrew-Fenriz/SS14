using Content.Server.Popups;
using Content.Server.Tabletop;
using Content.Shared.Administration;
using Content.Shared.Administration.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Tabletop.Components;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private SharedGodmodeSystem _sharedGodmodeSystem = default!;
    [Dependency] private InventorySystem _inventorySystem = default!;
    [Dependency] private PopupSystem _popupSystem = default!;
    [Dependency] private TabletopSystem _tabletopSystem = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;
    [Dependency] private AdminSmiteSystem _smiteSystem = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;

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
