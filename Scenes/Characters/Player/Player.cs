
using Godot;
using System;
using System.Collections.Generic;

public partial class Player : CharacterBody3D
{
    [ExportGroup("Melee")]
    [Export] public float AttackDamage { get; set; } = 20.0f;
    [Export] public float AttackDistance { get; set; } = 1.8f;
    [Export] public float AttackKnockback { get; set; } = 5.0f;

    public const float Speed = 5.0f;
    public const float JumpVelocity = 4.5f;

    public bool MovementLocked { get; set; }
    public bool CameraLocked { get; set; }

    public event Action<ThrowableObject> ObjectPickedUp;

    private Camera3D _camera;
    private RayCast3D _interactRay;
    private Node3D _holdPoint;
    private ThrowableObject _heldObject;
    private IInteractable _currentInteractable;
    private Label3D _interactionLabel;

    private float _cameraPitch;
    private float _throwCharge;
    private uint _heldCollisionLayer;
    private uint _heldCollisionMask;

    private const float MinThrowForce = 5.0f;
    private const float MaxThrowForce = 20.0f;
    private const float ThrowChargeSpeed = 15.0f;

    private CanvasLayer _throwChargeBarCanvas;
    private ProgressBar _throwChargeBar;

    private readonly Godot.Collections.Array<Rid> _heldObjectExclusions = new();
    private readonly Godot.Collections.Array<CollisionShape3D> _heldShapes = new();

    private const uint EnvironmentCollisionLayer = 1u << (7 - 1);
    private const float HoldCollisionSkin = 0.05f;

    private PunchingController _punchingController;
    private Basis _heldObjectInitialRotationLocal;

    private readonly List<Transform3D> _heldShapeLocals = new();

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;

        _camera = GetNode<Camera3D>("Camera3D");
        _interactRay = GetNode<RayCast3D>("Camera3D/InteractRay");
        _holdPoint = GetNode<Node3D>("Camera3D/HoldPoint");

        _punchingController = GetNode<PunchingController>("Punching");
        _punchingController.PunchHitFrame += TryPunchTarget;

        _interactionLabel = GetNode<Label3D>("InteractionLabel/Label3D");
        _interactionLabel.Hide();

        _throwChargeBarCanvas = GetNode<CanvasLayer>("ThrowChargeBar");
        _throwChargeBar = _throwChargeBarCanvas
            .GetNode<ProgressBar>("MarginContainer/ProgressBar");

        _throwChargeBarCanvas.Hide();
    }

    public override void _Input(InputEvent @event)
    {
        if (CameraLocked)
            return;

        if (@event is InputEventMouseMotion mouseMotion)
        {
            RotateY(-mouseMotion.Relative.X * 0.01f);

            _cameraPitch -= mouseMotion.Relative.Y * 0.01f;
            _cameraPitch = Mathf.Clamp(
                _cameraPitch,
                Mathf.DegToRad(-80),
                Mathf.DegToRad(80)
            );

            _camera.Rotation = new Vector3(_cameraPitch, 0, 0);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton
            && mouseButton.ButtonIndex == MouseButton.Left
            && mouseButton.Pressed
            && !mouseButton.IsEcho())
        {
            if (_heldObject == null && CanStartPunch())
            {
                if (_punchingController.StartPunch())
                    GetViewport().SetInputAsHandled();
            }

            return;
        }

        if (!@event.IsActionPressed("interact") || IsCinematicDialogueActive())
            return;

        GetViewport().SetInputAsHandled();

        if (_heldObject != null)
        {
            DropObject();
        }
        else if (!MovementLocked)
        {
            UpdateInteractionTarget();
            TryInteract();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (MovementLocked)
        {
            Velocity = Vector3.Zero;
            return;
        }

        Vector3 velocity = Velocity;

        if (!IsOnFloor())
            velocity += GetGravity() * (float)delta;

        if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
            velocity.Y = JumpVelocity;

        Vector2 inputDir = Input.GetVector(
            "move_left",
            "move_right",
            "move_forward",
            "move_backward"
        );

        Vector3 direction = (
            Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)
        ).Normalized();

        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * Speed;
            velocity.Z = direction.Z * Speed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
        }

        Velocity = velocity;
        MoveAndSlide();

        PushRigidBodies();
    }

    public override void _Process(double delta)
    {
        if (_heldObject != null)
            UpdateHeldObjectPose((float)delta);

        if (!IsCinematicDialogueActive())
            UpdateInteractionTarget();

        if (_heldObject == null)
            return;

        if (Input.IsActionJustPressed("throw"))
        {
            _throwCharge = MinThrowForce;
            _throwChargeBarCanvas.Show();
        }

        if (Input.IsActionPressed("throw"))
        {
            _throwCharge = Mathf.MoveToward(
                _throwCharge,
                MaxThrowForce,
                ThrowChargeSpeed * (float)delta
            );
        }

        float chargePercent = Mathf.InverseLerp(
            MinThrowForce,
            MaxThrowForce,
            _throwCharge
        ) * 100.0f;

        _throwChargeBar.Value = chargePercent;

        if (Input.IsActionJustReleased("throw"))
            ThrowObject();
    }

    private bool IsCinematicDialogueActive()
    {
        var controller = GetTree().CurrentScene
            ?.GetNodeOrNull<CutsceneController>("CutsceneController");

        return controller?.IsCinematicDialogueActive == true;
    }

    private void UpdateInteractionTarget()
    {
        IInteractable nextTarget = null;

        if (!MovementLocked
            && !IsCinematicDialogueActive()
            && _heldObject == null
            && _interactRay.IsColliding())
        {
            var interactable = FindInteractable(
                _interactRay.GetCollider() as Node
            );

            if (interactable != null && interactable.CanInteract(this))
                nextTarget = interactable;
        }

        if (ReferenceEquals(nextTarget, _currentInteractable))
        {
            if (nextTarget != null)
                UpdateLabelPosition(nextTarget);

            return;
        }

        _currentInteractable = nextTarget;

        if (_currentInteractable == null)
        {
            _interactionLabel.Hide();
            return;
        }

        _interactionLabel.Text =
            $"{_currentInteractable.InteractionText} [E]";

        UpdateLabelPosition(_currentInteractable);
        _interactionLabel.Show();
    }

    private void UpdateLabelPosition(IInteractable interactable)
    {
        if (interactable is not Node3D node
            || !GodotObject.IsInstanceValid(node))
        {
            _interactionLabel.Hide();
            return;
        }

        float eyeHeight = _camera.GlobalPosition.Y;
        float objectHeight = node.GlobalPosition.Y
            + interactable.PromptOffset.Y;

        float labelHeight = Mathf.Min(objectHeight, eyeHeight);

        _interactionLabel.GlobalPosition = new Vector3(
            node.GlobalPosition.X + interactable.PromptOffset.X,
            labelHeight,
            node.GlobalPosition.Z + interactable.PromptOffset.Z
        );
    }

    private void TryInteract()
    {
        if (_currentInteractable == null)
            return;

        var interactable = _currentInteractable;

        if (!interactable.CanInteract(this))
            return;

        _currentInteractable = null;
        _interactionLabel.Hide();

        interactable.Interact(this);
    }

    private static IInteractable FindInteractable(Node node)
    {
        for (Node current = node; current != null; current = current.GetParent())
        {
            if (current is IInteractable interactable)
                return interactable;
        }

        return null;
    }

    public bool TryPickup(ThrowableObject body)
    {
        if (_heldObject != null || !GodotObject.IsInstanceValid(body))
            return false;

        _heldObject = body;

        _heldObjectInitialRotationLocal =
            _holdPoint.GlobalBasis.Inverse() * body.GlobalBasis;

        _heldShapes.Clear();
        _heldShapeLocals.Clear();

        Transform3D worldToBody = body.GlobalTransform.AffineInverse();

        foreach (Node node in body.FindChildren("*", "CollisionShape3D", true, false))
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
        _heldObjectExclusions.Add(GetRid());
        _heldObjectExclusions.Add(body.GetRid());

        _heldCollisionLayer = body.CollisionLayer;
        _heldCollisionMask = body.CollisionMask;

        body.Freeze = true;
        body.PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;
        body.CollisionLayer = 0;
        body.CollisionMask = 0;

        ObjectPickedUp?.Invoke(body);

        return true;
    }

    private void ThrowObject()
    {
        if (_heldObject == null)
            return;

        var throwDirection = -_camera.GlobalTransform.Basis.Z;

        _heldObject.ThrowForce = _throwCharge;
        _heldObject.PhysicsInterpolationMode = PhysicsInterpolationModeEnum.On;
        _heldObject.Freeze = false;
        _heldObject.CollisionLayer = _heldCollisionLayer;
        _heldObject.CollisionMask = _heldCollisionMask | 2;

        _heldObject.ApplyCentralImpulse(throwDirection * _throwCharge);

        ClearHeldObjectState();
        ResetChargeBar();
    }

    private void DropObject()
    {
        if (_heldObject == null)
            return;

        _heldObject.Freeze = false;
        _heldObject.PhysicsInterpolationMode = PhysicsInterpolationModeEnum.On;
        _heldObject.CollisionLayer = _heldCollisionLayer;
        _heldObject.CollisionMask = _heldCollisionMask;

        ClearHeldObjectState();
        ResetChargeBar();
    }

    private void ClearHeldObjectState()
    {
        _heldObjectExclusions.Clear();
        _heldShapes.Clear();
        _heldShapeLocals.Clear();
        _heldObject = null;
    }

    private void ResetChargeBar()
    {
        _throwCharge = 0;
        _throwChargeBar.Value = 0;
        _throwChargeBarCanvas.Hide();
    }

    private void PushRigidBodies()
    {
        for (var i = 0; i < GetSlideCollisionCount(); i++)
        {
            var collision = GetSlideCollision(i);

            if (collision.GetCollider() is not RigidBody3D rigidBody)
                continue;

            var pushDirection = -collision.GetNormal();
            pushDirection.Y = 0;

            if (pushDirection.LengthSquared() == 0)
                continue;

            pushDirection = pushDirection.Normalized();
            rigidBody.ApplyCentralImpulse(pushDirection * 2.0f);
        }
    }

    private void UpdateHeldObjectPose(float delta)
    {
        if (_heldObject == null
            || !GodotObject.IsInstanceValid(_heldObject)
            || _holdPoint == null
            || _camera == null
            || GetWorld3D()?.DirectSpaceState == null)
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

        var spaceState = GetWorld3D()?.DirectSpaceState;

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

        var spaceState = GetWorld3D()?.DirectSpaceState;

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
        if (index >= _heldShapeLocals.Count
            || index >= _heldShapes.Count)
        {
            return false;
        }

        CollisionShape3D shape = _heldShapes[index];

        return GodotObject.IsInstanceValid(shape)
            && !shape.Disabled
            && shape.Shape != null;
    }

    private bool CanStartPunch()
    {
        if (MovementLocked || _punchingController.IsPlaying)
            return false;

        return !IsCinematicDialogueActive();
    }

    private void TryPunchTarget()
    {
        if (MovementLocked || _heldObject != null)
            return;

        if (IsCinematicDialogueActive())
            return;

        Vector3 origin = _camera.GlobalPosition;
        Vector3 direction = -_camera.GlobalTransform.Basis.Z;
        Vector3 end = origin + direction * AttackDistance;

        var query = PhysicsRayQueryParameters3D.Create(origin, end);

        query.Exclude = new Godot.Collections.Array<Rid>
        {
            GetRid()
        };

        query.CollideWithAreas = false;

        var result = GetWorld3D()
            .DirectSpaceState
            .IntersectRay(query);

        if (result.Count == 0)
            return;

        var collider = result["collider"].AsGodotObject() as Node;

        if (collider == null)
            return;

        var interactable = FindInteractable(collider);

        if (interactable is not InteractableCharacter character)
            return;

        character.NotifyHitByPunch(
            this,
            AttackDamage,
            AttackKnockback
        );
    }

    public override void _ExitTree()
    {
        if (_punchingController != null
            && GodotObject.IsInstanceValid(_punchingController))
        {
            _punchingController.PunchHitFrame -= TryPunchTarget;
        }
    }
}
