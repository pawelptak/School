using Godot;
using System;

public partial class Player : CharacterBody3D
{
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

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;

        _camera = GetNode<Camera3D>("Camera3D");
        _interactRay = GetNode<RayCast3D>("Camera3D/InteractRay");
        _holdPoint = GetNode<Node3D>("Camera3D/HoldPoint");

        _interactionLabel = GetNode<Label3D>("InteractionLabel/Label3D");

        _interactionLabel.Hide();

        _throwChargeBarCanvas = GetNode<CanvasLayer>("ThrowChargeBar");
        _throwChargeBar = _throwChargeBarCanvas.GetNode<ProgressBar>("MarginContainer/ProgressBar");
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
        if (!@event.IsActionPressed("interact") || IsDialogueVisible())
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
        if (!IsDialogueVisible())
            UpdateInteractionTarget();

        if (_heldObject == null)
            return;

        _heldObject.GlobalPosition = GetSafeHoldPosition();

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

    private bool IsDialogueVisible()
    {
        var controller = GetTree().CurrentScene
            ?.GetNodeOrNull<CutsceneController>("CutsceneController");

        return controller?.IsDialogueVisible == true;
    }

    private void UpdateInteractionTarget()
    {
        IInteractable nextTarget = null;

        if (!MovementLocked && !IsDialogueVisible() && _heldObject == null
            && _interactRay.IsColliding())
        {
            if (_interactRay.GetCollider() is IInteractable interactable
                && interactable.CanInteract(this))
            {
                nextTarget = interactable;
            }
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

        _interactionLabel.Text = $"{_currentInteractable.InteractionText} [E]";
        UpdateLabelPosition(_currentInteractable);
        _interactionLabel.Show();
    }

    private void UpdateLabelPosition(IInteractable interactable)
    {
        if (interactable is not Node3D node ||
            !GodotObject.IsInstanceValid(node))
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

    public bool TryPickup(ThrowableObject body)
    {
        if (_heldObject != null || !GodotObject.IsInstanceValid(body))
            return false;

        _heldObject = body;

        _heldObjectExclusions.Clear();
        _heldObjectExclusions.Add(GetRid());
        _heldObjectExclusions.Add(body.GetRid());

        _heldCollisionLayer = body.CollisionLayer;
        _heldCollisionMask = body.CollisionMask;

        body.Freeze = true;
        body.CollisionLayer = 0;
        body.CollisionMask = 0;
        body.GlobalPosition = _holdPoint.GlobalPosition;

        ObjectPickedUp?.Invoke(body);
        return true;
    }

    private void ThrowObject()
    {
        if (_heldObject == null)
            return;

        var throwDirection = -_camera.GlobalTransform.Basis.Z;

        _heldObject.ThrowForce = _throwCharge;
        _heldObject.Freeze = false;
        _heldObject.CollisionLayer = _heldCollisionLayer;
        _heldObject.CollisionMask = _heldCollisionMask | 2;
        _heldObject.ApplyCentralImpulse(throwDirection * _throwCharge);

        _heldObjectExclusions.Clear();
        _heldObject = null;

        ResetChargeBar();
    }

    private void DropObject()
    {
        if (_heldObject == null)
            return;

        _heldObject.Freeze = false;
        _heldObject.CollisionLayer = _heldCollisionLayer;
        _heldObject.CollisionMask = _heldCollisionMask;

        _heldObjectExclusions.Clear();
        _heldObject = null;

        ResetChargeBar();
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



    private Vector3 GetSafeHoldPosition()
    {
        Vector3 origin = _camera.GlobalPosition;
        Vector3 target = _holdPoint.GlobalPosition;
        Vector3 motion = target - _heldObject.GlobalPosition;

        if (motion.LengthSquared() < 0.000001f)
            return _heldObject.GlobalPosition;

        var spaceState = GetWorld3D().DirectSpaceState;
        var shapes = _heldObject.FindChildren(
            "*",
            "CollisionShape3D",
            true,
            false
        );

        float safeFraction = 1.0f;

        foreach (Node node in shapes)
        {
            var collisionShape = node as CollisionShape3D;

            if (collisionShape == null ||
                collisionShape.Shape == null ||
                collisionShape.Disabled)
            {
                continue;
            }

            Transform3D relativeTransform =
                _heldObject.GlobalTransform.AffineInverse()
                * collisionShape.GlobalTransform;

            var query = new PhysicsShapeQueryParameters3D
            {
                Shape = collisionShape.Shape,
                Transform = new Transform3D(
                    _heldObject.GlobalBasis,
                    _heldObject.GlobalPosition
                ) * relativeTransform,
                Motion = motion,
                CollisionMask = 1,
                Exclude = _heldObjectExclusions
            };

            var result = spaceState.CastMotion(query);
            safeFraction = Mathf.Min(safeFraction, result[0]);
        }

        safeFraction = Mathf.Max(0.0f, safeFraction - 0.02f);

        return _heldObject.GlobalPosition + motion * safeFraction;
    }
}
