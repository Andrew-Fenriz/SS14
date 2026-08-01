using System.Numerics;
using Content.Shared.Buckle.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared.Graphics.RSI;

namespace Content.Client.Buckle;

internal sealed partial class BuckleSystem
{
    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        // Eye rotation has no change event, and rotating a parent grid does not necessarily move the strap itself.
        // Check active trackers every frame, but only touch the sprite when its apparent cardinal direction changes.
        var query = EntityQueryEnumerator<ActiveStrapVisualsComponent, BuckleComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var active, out var buckle, out var sprite))
        {
            if (buckle.BuckledTo != active.Strap ||
                !TryComp<StrapComponent>(active.Strap, out _) ||
                !TryComp<StrapVisualsComponent>(active.Strap, out var visuals))
            {
                RemCompDeferred(uid, active);
                continue;
            }
            RefreshStrapVisuals((uid, sprite), active, visuals);
        }
    }

    private void ApplyStrapVisuals(Entity<BuckleComponent> buckle, Entity<StrapComponent> strap)
    {
        if (!TryComp<StrapVisualsComponent>(strap, out var visuals) ||
            !TryComp<SpriteComponent>(buckle, out var sprite))
        {
            RemoveStrapVisuals(buckle);
            return;
        }

        var active = EnsureComp<ActiveStrapVisualsComponent>(buckle);
        if (active.Strap != strap.Owner)
            ResetStrapVisuals((buckle, sprite), active);

        active.Strap = strap;
        RefreshStrapVisuals((buckle, sprite), active, visuals);
    }

    private void RefreshStrapVisuals(
        Entity<SpriteComponent?> buckle,
        ActiveStrapVisualsComponent active,
        StrapVisualsComponent visuals)
    {
        var direction = GetStrapScreenDirection(active.Strap);
        if (active.Direction == direction)
            return;

        var directionVisuals = visuals.Directions.GetValueOrDefault(direction);
        ApplyDirectionVisuals(buckle, active, direction, directionVisuals);
    }

    private Direction GetStrapScreenDirection(EntityUid strap)
    {
        // Match SpriteSystem.RenderSprite exactly so the offset and the directional RSI frame change together.
        // In particular, Layer.GetDirection applies the renderer's bias around diagonal boundaries.
        var rotation = (_xformSystem.GetWorldRotation(strap) + _eye.CurrentEye.Rotation)
            .Reduced()
            .FlipPositive();
        return SpriteComponent.Layer.GetDirection(RsiDirectionType.Dir4, rotation).Convert();
    }

    private void ApplyDirectionVisuals(
        Entity<SpriteComponent?> buckle,
        ActiveStrapVisualsComponent active,
        Direction direction,
        StrapDirectionVisuals? visuals)
    {
        var pixelOffset = visuals?.PixelOffset ?? Vector2i.Zero;
        var offset = (Vector2) pixelOffset / EyeManager.PixelsPerMeter;
        var baseOffset = buckle.Comp!.Offset - active.AppliedOffset;
        _sprite.SetOffset(buckle, baseOffset + offset);

        active.Direction = direction;
        active.AppliedOffset = offset;
    }

    private void RemoveStrapVisuals(EntityUid buckle)
    {
        if (HasComp<ActiveStrapVisualsComponent>(buckle))
            RemComp<ActiveStrapVisualsComponent>(buckle);
    }

    [SubscribeLocalEvent]
    private void OnActiveStrapVisualsShutdown(
        Entity<ActiveStrapVisualsComponent> ent,
        ref ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
            ResetStrapVisuals((ent, sprite), ent.Comp);
    }

    private void ResetStrapVisuals(
        Entity<SpriteComponent?> buckle,
        ActiveStrapVisualsComponent active)
    {
        if (active.AppliedOffset != Vector2.Zero)
            _sprite.SetOffset(buckle, buckle.Comp!.Offset - active.AppliedOffset);

        active.AppliedOffset = Vector2.Zero;
        active.Direction = null;
    }
}
