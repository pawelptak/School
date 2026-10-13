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

    private CanvasLayer _interactionUiCanvas;
    private Label _interactionTextLabel;
    private ProgressBar _interactionProgressBar;

    private HeldObjectController _heldObjectController;
    private PunchingController _punchingController;

    private float _cameraPitch;
    private float _throwCharge;

    private const float MinThrowForce = 5.0f;
    private const float MaxThrowForce = 20.0f;
    private const float ThrowChargeSpeed = 15.0f;

    private CanvasLayer _throwChargeBarCanvas;
    private ProgressBar _throwChargeBar;

    private float _interactionHoldTime = 0.0f;
    private float _requiredInteractionTime = 1.0f;
    private bool _isHoldingInteraction = false;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;

        _camera = GetNode<Camera3D>("Camera3D");
        _interactRay = GetNode<RayCast3D>("Camera3D/InteractRay");

        _heldObjectController = GetNode<HeldObjectController>("HeldObjectController");
        _heldObjectController.ObjectPickedUp += OnObjectPickedUp;

        _punchingController = GetNode<PunchingController>("Punching");
        _punchingController.PunchHitFrame += TryPunchTarget;

        // Inicjalizacja CanvasLayer UI (dostosuj ścieżki do swojej sceny)
        _interactionUiCanvas = GetNode<CanvasLayer>("InteractionUI");
        _interactionTextLabel = GetNode<Label>("InteractionUI/Container/VBoxContainer/Label");
        _interactionProgressBar = GetNode<ProgressBar>("InteractionUI/Container/VBoxContainer/InteractionProgressBar");

        _interactionUiCanvas.Hide();
        if (_interactionProgressBar != null)
            _interactionProgressBar.Modulate = new Color(1, 1, 1, 0);
        //_interactionProgressBar.Hide();

        _throwChargeBarCanvas = GetNode<CanvasLayer>("ThrowChargeBar");
        _throwChargeBar = _throwChargeBarCanvas.GetNode<ProgressBar>("MarginContainer/ProgressBar");
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
            if (_heldObjectController.HeldObject == null && CanStartPunch())
            {
                if (_punchingController.StartPunch())
                    GetViewport().SetInputAsHandled();
            }

            return;
        }

        if (@event.IsActionPressed("interact") && !IsCinematicDialogueActive())
        {
            if (_heldObjectController.HeldObject != null)
            {
                GetViewport().SetInputAsHandled();
                _heldObjectController.DropObject();
                ResetChargeBar();
                ResetHoldInteraction();
                return;
            }

            if (_currentInteractable != null && !_currentInteractable.RequiresHold)
            {
                GetViewport().SetInputAsHandled();
                TryInteract();
            }
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
        _heldObjectController.UpdateHeldObjectPose((float)delta);

        if (!IsCinematicDialogueActive())
            UpdateInteractionTarget();

        HandleHoldInteraction((float)delta);

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

    private void HandleHoldInteraction(float delta)
    {
        if (_heldObjectController.HeldObject == null && !MovementLocked && !IsCinematicDialogueActive())
        {
            if (Input.IsActionPressed("interact") && _currentInteractable != null && _currentInteractable.RequiresHold)
            {
                if (!_isHoldingInteraction)
                {
                    _isHoldingInteraction = true;
                    _interactionHoldTime = 0.0f;
                    _requiredInteractionTime = _currentInteractable.GetInteractionDuration();
                }

                _interactionHoldTime += delta;
                float progress = Mathf.Clamp(_interactionHoldTime / _requiredInteractionTime, 0.0f, 1.0f);

                if (_interactionProgressBar != null)
                {
                    _interactionProgressBar.Modulate = new Color(1, 1, 1, 1);
                    _interactionProgressBar.Value = progress * 100.0f;
                }

                if (_interactionHoldTime >= _requiredInteractionTime)
                {
                    TryInteract();
                    ResetHoldInteraction();
                }
                return;
            }
        }

        if (Input.IsActionJustReleased("interact") || _currentInteractable == null || !_currentInteractable.RequiresHold)
        {
            ResetHoldInteraction();
        }
    }

    private void ResetHoldInteraction()
    {
        _isHoldingInteraction = false;
        _interactionHoldTime = 0.0f;

        if (_interactionProgressBar != null)
        {
            _interactionProgressBar.Value = 0;

            if (_currentInteractable is not ThrowableObject)
            {
                _interactionProgressBar.Modulate = new Color(1, 1, 1, 0);
            }
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
            var interactable = FindInteractable(_interactRay.GetCollider() as Node);

            if (interactable != null && interactable.CanInteract(this))
                nextTarget = interactable;
        }

        if (ReferenceEquals(nextTarget, _currentInteractable))
            return;

        ResetHoldInteraction();
        _currentInteractable = nextTarget;

        if (_currentInteractable == null)
        {
            _interactionUiCanvas.Hide();
            return;
        }

        _interactionTextLabel.Text = _currentInteractable.GetPromptText();

        if (_interactionProgressBar != null)
            _interactionProgressBar.Modulate = new Color(1, 1, 1, 0);

        _interactionUiCanvas.Show();
    }

    private void TryInteract()
    {
        if (_currentInteractable == null)
            return;

        var interactable = _currentInteractable;

        if (!interactable.CanInteract(this))
            return;

        _currentInteractable = null;
        _interactionUiCanvas.Hide();

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
