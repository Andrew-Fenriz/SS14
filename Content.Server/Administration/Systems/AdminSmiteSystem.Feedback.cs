using Content.Shared.Administration.Smites;
using Robust.Shared.Player;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminSmiteSystem
{
    [SubscribeLocalEvent]
    private void OnPopup(Entity<MetaDataComponent> entity, ref SmiteOperationEvent<PopupSmite> args)
    {
        var message = Loc.GetString(args.Operation.Message,
            ("name", entity.Owner),
            ("entity", entity.Owner));

        switch ((args.Operation.Recipients, args.Operation.Location))
        {
            case (SmitePopupRecipients.Target, SmitePopupLocation.Entity):
                _popup.PopupEntity(message, entity, entity, args.Operation.Type);
                break;
            case (SmitePopupRecipients.Target, SmitePopupLocation.Coordinates):
                _popup.PopupCoordinates(message, Transform(entity).Coordinates, entity, args.Operation.Type);
                break;
            case (SmitePopupRecipients.Pvs, SmitePopupLocation.Entity):
                _popup.PopupEntity(message, entity, args.Operation.Type);
                break;
            case (SmitePopupRecipients.Pvs, SmitePopupLocation.Coordinates):
                _popup.PopupCoordinates(message, Transform(entity).Coordinates, args.Operation.Type);
                break;
            case (SmitePopupRecipients.PvsExceptTarget, SmitePopupLocation.Entity):
                _popup.PopupEntity(message, entity, Filter.PvsExcept(entity), true, args.Operation.Type);
                break;
            case (SmitePopupRecipients.PvsExceptTarget, SmitePopupLocation.Coordinates):
                _popup.PopupCoordinates(
                    message,
                    Transform(entity).Coordinates,
                    Filter.PvsExcept(entity),
                    true,
                    args.Operation.Type);
                break;
        }
    }
}
