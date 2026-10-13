
using Godot;
using System;

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
    private IInteractable _currentInteractable;
    private Label3D _interactionLabel;

    private HeldObjectController _heldObjectController;
    private PunchingController _punchingController;

    private float _cameraPitch;
    private float _throwCharge;

    private const float MinThrowForce = 5.0f;
    private const float MaxThrowForce = 20.0f;
    private const float ThrowChargeSpeed = 15.0f;

    private CanvasLayer _throwChargeBarCanvas;
    private ProgressBar _throwChargeBar;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;

        _camera = GetNode<Camera3D>("Camera3D");
        _interactRay = GetNode<RayCast3D>("Camera3D/InteractRay");

        _heldObjectController =
            GetNode<HeldObjectController>("HeldObjectController");

        _heldObjectController.ObjectPickedUp += OnObjectPickedUp;

        _punchingController = GetNode<PunchingController>("Punching");
        _punchingController.PunchHitFrame += TryPunchTarget;

        _interactionLabel = GetNode<Label3D>("InteractionLabel/Label3D");
        _interactionLabel.Hide();

        _throwChargeBarCanvas = GetNode<CanvasLayer>("ThrowChargeBar");
        _throwChargeBar = _throwChargeBarCanvas
            .GetNode<ProgressBar>("MarginContainer/ProgressBar");

        _throwChargeBarCanvas.Hide();
    }

    private void OnObjectPickedUp(ThrowableObject body)
    {
        ObjectPickedUp?.Invoke(body);
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
            if (_heldObjectController.HeldObject == null
                && CanStartPunch())
            {
                if (_punchingController.StartPunch())
                    GetViewport().SetInputAsHandled();
            }

            return;
        }

        if (!@event.IsActionPressed("interact")
            || IsCinematicDialogueActive())
        {
            return;
        }

        GetViewport().SetInputAsHandled();

        if (_heldObjectController.HeldObject != null)
        {
            _heldObjectController.DropObject();
            ResetChargeBar();
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
        // Aktualizujemy trzymany obiekt w tym samym cyklu co kamerę.
        // To zapobiega drganiom przy ruchu myszką.
        _heldObjectController.UpdateHeldObjectPose((float)delta);

        if (!IsCinematicDialogueActive())
            UpdateInteractionTarget();

        if (_heldObjectController.HeldObject == null)
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
        {
            _heldObjectController.ThrowObject(_throwCharge);
            ResetChargeBar();
        }
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
            && _heldObjectController.HeldObject == null
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
        if (MovementLocked || IsCinematicDialogueActive())
            return false;

        return _heldObjectController.TryPickup(body);
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

    private bool CanStartPunch()
    {
        if (MovementLocked || _punchingController.IsPlaying)
            return false;

        return !IsCinematicDialogueActive();
    }

    private void TryPunchTarget()
    {
        if (MovementLocked
            || _heldObjectController.HeldObject != null)
        {
            return;
        }

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
        if (_heldObjectController != null
            && GodotObject.IsInstanceValid(_heldObjectController))
        {
            _heldObjectController.ObjectPickedUp -= OnObjectPickedUp;
        }

        if (_punchingController != null
            && GodotObject.IsInstanceValid(_punchingController))
        {
            _punchingController.PunchHitFrame -= TryPunchTarget;
        }
    }
}
