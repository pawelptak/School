
using Godot;
using System;
using System.Collections.Generic;

public partial class HeldObjectController : Node
{
    [ExportGroup("Collision")]
    [Export] public uint EnvironmentCollisionLayer { get; set; } = 1u << 6;
    [Export] public float HoldCollisionSkin { get; set; } = 0.05f;

    public ThrowableObject HeldObject => _heldObject;

    public event Action<ThrowableObject> ObjectPickedUp;

    private Player _player;
    private Camera3D _camera;
    private Node3D _holdPoint;

    private ThrowableObject _heldObject;

    private uint _heldCollisionLayer;
    private uint _heldCollisionMask;

    private PhysicsInterpolationModeEnum _originalInterpolationMode;

    private Basis _heldObjectInitialRotationLocal;

    private readonly Godot.Collections.Array<Rid> _heldObjectExclusions = new();
    private readonly Godot.Collections.Array<CollisionShape3D> _heldShapes = new();
    private readonly List<Transform3D> _heldShapeLocals = new();

    [Signal]
    public delegate void ObjectThrownEventHandler(ThrowableObject thrownObject);

    public override void _Ready()
    {
        _player = GetParent<Player>();

        if (_player == null)
        {
            GD.PushError("HeldObjectController musi być dzieckiem Player.");
            return;
        }

        _camera = _player.GetNode<Camera3D>("Camera3D");
        _holdPoint = _player.GetNode<Node3D>("Camera3D/HoldPoint");
    }

    public bool TryPickup(ThrowableObject body)
    {
        if (_heldObject != null
            || !GodotObject.IsInstanceValid(body)
            || _player == null)
        {
            return false;
        }

        _heldObject = body;

        _heldObjectInitialRotationLocal =
            _holdPoint.GlobalBasis.Inverse() * body.GlobalBasis;

        _heldShapes.Clear();
        _heldShapeLocals.Clear();

        Transform3D worldToBody = body.GlobalTransform.AffineInverse();

        foreach (Node node in body.FindChildren(
            "*",
            "CollisionShape3D",
            true,
            false))
        {
            if (node is CollisionShape3D shape
                && shape.Shape != null
                && !shape.Disabled)
            {
                _heldShapes.Add(shape);
                _heldShapeLocals.Add(
                    worldToBody * shape.GlobalTransform
                );
            }
        }

        _heldObjectExclusions.Clear();
        _heldObjectExclusions.Add(_player.GetRid());
        _heldObjectExclusions.Add(body.GetRid());

        _heldCollisionLayer = body.CollisionLayer;
        _heldCollisionMask = body.CollisionMask;

        _originalInterpolationMode = body.PhysicsInterpolationMode;

        body.Freeze = true;
        body.CollisionLayer = 0;
        body.CollisionMask = 0;
        body.PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;

        ObjectPickedUp?.Invoke(body);
        body.SetHeldTransparency(true);

        return true;
    }

    public void UpdateHeldObjectPose(float delta)
    {
        if (_heldObject == null
            || !GodotObject.IsInstanceValid(_heldObject)
            || _holdPoint == null
            || _camera == null
            || GetWorld()?.DirectSpaceState == null)
        {
            return;
        }

        Vector3 previousPosition = _heldObject.GlobalPosition;
        Basis previousBasis = _heldObject.GlobalBasis;

        Vector3 desiredPosition = _holdPoint.GlobalPosition;
        Basis desiredBasis =
            _holdPoint.GlobalBasis * _heldObjectInitialRotationLocal;

        _heldObject.GlobalBasis = desiredBasis;

        if (TryPlaceHeld(previousPosition, desiredPosition, out Vector3 target))
        {
            _heldObject.GlobalPosition = target;
            return;
        }

        _heldObject.GlobalBasis = previousBasis;

        if (TryPlaceHeld(previousPosition, desiredPosition, out target))
        {
            _heldObject.GlobalPosition = target;
            return;
        }

        _heldObject.GlobalPosition = previousPosition;
    }

    public void DropObject()
    {
        if (_heldObject == null)
            return;

        ReleaseHeldObject(restoreCollisionMask: _heldCollisionMask);
    }

    public void ThrowObject(float throwForce)
    {
        if (_heldObject == null || _camera == null)
            return;

        ThrowableObject body = _heldObject;

        Vector3 throwDirection = -_camera.GlobalTransform.Basis.Z;

        ReleaseHeldObject(
            restoreCollisionMask: _heldCollisionMask | 2u
        );

        body.ThrowForce = throwForce;
        body.ApplyCentralImpulse(throwDirection * throwForce);

        EmitSignal(SignalName.ObjectThrown, body);
    }

    private void ReleaseHeldObject(uint restoreCollisionMask)
    {
        ThrowableObject body = _heldObject;

        if (body == null)
            return;

        body.CollisionLayer = _heldCollisionLayer;
        body.CollisionMask = restoreCollisionMask;
        body.PhysicsInterpolationMode = _originalInterpolationMode;
        body.Freeze = false;

        _heldObjectExclusions.Clear();
        _heldShapes.Clear();
        _heldShapeLocals.Clear();

        _heldObject = null;
        body.SetHeldTransparency(false);
    }

    private bool TryPlaceHeld(
        Vector3 previousPosition,
        Vector3 desiredPosition,
        out Vector3 solved)
    {
        if (!HeldOverlapsAt(desiredPosition))
        {
            if (!HeldOverlapsAt(previousPosition))
            {
                solved = SweepHeld(previousPosition, desiredPosition);

                if (!HeldOverlapsAt(solved))
                    return true;
            }
            else if (TryRetreatUntilClear(
                previousPosition,
                _camera.GlobalPosition,
                out Vector3 clear))
            {
                solved = SweepHeld(clear, desiredPosition);

                if (!HeldOverlapsAt(solved))
                    return true;
            }
        }

        if (!HeldOverlapsAt(previousPosition))
        {
            solved = SweepHeld(previousPosition, desiredPosition);

            if (!HeldOverlapsAt(solved))
                return true;
        }

        if (!TryRetreatUntilClear(
            previousPosition,
            _camera.GlobalPosition,
            out Vector3 retreatClear))
        {
            solved = previousPosition;
            return false;
        }

        solved = SweepHeld(retreatClear, desiredPosition);

        if (!HeldOverlapsAt(solved))
            return true;

        return TryRetreatUntilClear(
            solved,
            retreatClear,
            out solved
        );
    }

    private bool TryRetreatUntilClear(
        Vector3 from,
        Vector3 to,
        out Vector3 clear)
    {
        if (!HeldOverlapsAt(from))
        {
            clear = from;
            return true;
        }

        if (HeldOverlapsAt(to))
        {
            clear = from;
            return false;
        }

        float lo = 0.0f;
        float hi = 1.0f;

        for (int i = 0; i < 10; i++)
        {
            float mid = (lo + hi) * 0.5f;

            if (HeldOverlapsAt(from.Lerp(to, mid)))
                lo = mid;
            else
                hi = mid;
        }

        float length = from.DistanceTo(to);
        float skin = length > 0.001f
            ? HoldCollisionSkin / length
            : 0.0f;

        clear = from.Lerp(
            to,
            Mathf.Clamp(hi + skin, 0.0f, 1.0f)
        );

        if (HeldOverlapsAt(clear))
            clear = from.Lerp(to, hi);

        return !HeldOverlapsAt(clear);
    }

    private Vector3 SweepHeld(Vector3 from, Vector3 to)
    {
        Vector3 motion = to - from;
        float distance = motion.Length();

        if (distance < 0.0001f || _heldShapes.Count == 0)
            return to;

        var spaceState = GetWorld()?.DirectSpaceState;

        if (spaceState == null)
            return from;

        float safeFraction = 1.0f;

        for (int i = 0; i < _heldShapes.Count; i++)
        {
            if (!IsHeldShapeUsable(i))
                continue;

            PhysicsShapeQueryParameters3D query = MakeShapeQuery(from, i);
            query.Motion = motion;

            float[] result = spaceState.CastMotion(query);
            safeFraction = Mathf.Min(safeFraction, result[0]);
        }

        safeFraction = Mathf.Clamp(safeFraction, 0.0f, 1.0f);

        if (safeFraction < 1.0f)
        {
            float safeDistance = Mathf.Max(
                0.0f,
                distance * safeFraction - HoldCollisionSkin
            );

            safeFraction = safeDistance / distance;
        }

        return from + motion * safeFraction;
    }

    private bool HeldOverlapsAt(Vector3 origin)
    {
        if (_heldShapes.Count == 0)
            return false;

        var spaceState = GetWorld()?.DirectSpaceState;

        if (spaceState == null)
            return true;

        for (int i = 0; i < _heldShapes.Count; i++)
        {
            if (!IsHeldShapeUsable(i))
                continue;

            PhysicsShapeQueryParameters3D query = MakeShapeQuery(origin, i);

            if (spaceState.IntersectShape(query, 1).Count > 0)
                return true;
        }

        return false;
    }

    private PhysicsShapeQueryParameters3D MakeShapeQuery(
        Vector3 origin,
        int shapeIndex)
    {
        return new PhysicsShapeQueryParameters3D
        {
            Shape = _heldShapes[shapeIndex].Shape,

            Transform = new Transform3D(
                _heldObject.GlobalBasis,
                origin
            ) * _heldShapeLocals[shapeIndex],

            CollisionMask = EnvironmentCollisionLayer,
            Exclude = _heldObjectExclusions,
            CollideWithAreas = false,
            CollideWithBodies = true,
            Margin = HoldCollisionSkin
        };
    }

    private bool IsHeldShapeUsable(int index)
    {
        if (index >= _heldShapes.Count
            || index >= _heldShapeLocals.Count)
        {
            return false;
        }

        CollisionShape3D shape = _heldShapes[index];

        return GodotObject.IsInstanceValid(shape)
            && !shape.Disabled
            && shape.Shape != null;
    }

    public override void _ExitTree()
    {
        if (_heldObject != null
            && GodotObject.IsInstanceValid(_heldObject))
        {
            ReleaseHeldObject(_heldCollisionMask);
        }
    }

    private World3D GetWorld()
    {
        return _player?.GetWorld3D();
    }

    public void ClearHeldObjectReference()
    {
        _heldObject = null;
        _heldObjectExclusions.Clear();
        _heldShapes.Clear();
        _heldShapeLocals.Clear();
    }
}
