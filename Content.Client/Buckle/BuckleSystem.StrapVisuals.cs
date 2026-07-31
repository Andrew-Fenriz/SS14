using System.Numerics;
using Content.Shared.Buckle.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Buckle;

internal sealed partial class BuckleSystem
{
    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        // Eye rotation has no change event, and rotating a parent grid does not necessarily move the strap itself.
        // Check active trackers every frame, but only touch the sprite when its apparent cardinal direction changes.
        var query = EntityQueryEnumerator<StrapVisualsOffsetComponent, BuckleComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var tracker, out var buckle, out var sprite))
        {
            if (buckle.BuckledTo != tracker.Strap ||
                !TryComp<StrapComponent>(tracker.Strap, out _) ||
                !TryComp<StrapVisualsComponent>(tracker.Strap, out var visuals))
            {
                RemCompDeferred(uid, tracker);
                continue;
            }

            var direction = GetStrapScreenDirection(tracker.Strap);
            if (tracker.Direction == direction)
                continue;

            var offset = visuals.Visuals.GetValueOrDefault(direction)?.Offset ?? Vector2.Zero;
            ApplyOffset((uid, sprite), tracker, direction, offset);
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

        var tracker = EnsureComp<StrapVisualsOffsetComponent>(buckle);
        if (tracker.Strap != strap.Owner)
            RemoveAppliedOffset((buckle, sprite), tracker);

        tracker.Strap = strap;

        var direction = GetStrapScreenDirection(strap);
        var offset = visuals.Visuals.GetValueOrDefault(direction)?.Offset ?? Vector2.Zero;
        ApplyOffset((buckle, sprite), tracker, direction, offset);
    }

    private Direction GetStrapScreenDirection(EntityUid strap)
    {
        var rotation = _xformSystem.GetWorldRotation(strap) + _eye.CurrentEye.Rotation;
        return rotation.GetCardinalDir();
    }

    private void ApplyOffset(
        Entity<SpriteComponent?> buckle,
        StrapVisualsOffsetComponent tracker,
        Direction direction,
        Vector2 offset)
    {
        var baseOffset = buckle.Comp!.Offset - tracker.AppliedOffset;
        _sprite.SetOffset(buckle, baseOffset + offset);

        tracker.Direction = direction;
        tracker.AppliedOffset = offset;
    }

    private void RemoveStrapVisuals(EntityUid buckle)
    {
        if (HasComp<StrapVisualsOffsetComponent>(buckle))
            RemComp<StrapVisualsOffsetComponent>(buckle);
    }

    [SubscribeLocalEvent]
    private void OnStrapVisualsTrackerShutdown(
        Entity<StrapVisualsOffsetComponent> ent,
        ref ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
            RemoveAppliedOffset((ent, sprite), ent.Comp);
    }

    private void RemoveAppliedOffset(
        Entity<SpriteComponent?> buckle,
        StrapVisualsOffsetComponent tracker)
    {
        if (tracker.AppliedOffset == Vector2.Zero)
            return;

        _sprite.SetOffset(buckle, buckle.Comp!.Offset - tracker.AppliedOffset);
        tracker.AppliedOffset = Vector2.Zero;
        tracker.Direction = null;
    }
}
