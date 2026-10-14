using Godot;
using System;
using System.Collections.Generic;

public partial class Player : CharacterBody3D
{
    [ExportGroup("Melee")]
    [Export] public float AttackDamage { get; set; } = 20.0f;
    [Export] public float AttackDistance { get; set; } = 1.8f;
    [Export] public float AttackKnockback { get; set; } = 5.0f;

    [ExportGroup("Throw")]
    [Export] public float FixedThrowForce { get; set; } = 20.0f;

    [ExportGroup("Inventory")]
    [Export] public int MaxInventorySlots { get; set; } = 9;

    private InventoryUI _inventoryUI;

    public const float Speed = 5.0f;
    public const float JumpVelocity = 4.5f;

    public bool MovementLocked { get; set; }
    public bool CameraLocked { get; set; }

    public event Action<ThrowableObject> ObjectPickedUp;

    private Camera3D _camera;
    private RayCast3D _interactRay;
    private IInteractable _currentInteractable;

    private CanvasLayer _interactionUiCanvas;
    private CanvasLayer _pickedUpObjectCanvas;
    private Label _interactionTextLabel;
    private ProgressBar _interactionProgressBar;

    private Label _throwLabel;
    private Label _takeLabel;

    private HeldObjectController _heldObjectController;
    private PunchingController _punchingController;

    // Struktura ekwipunku (publiczna, aby InventoryUI mogło z niej korzystać)
    public class InventoryItem
    {
        public string ItemName { get; set; }
        public PackedScene ObjectScene { get; set; }
        public Texture2D Icon { get; set; }
        public int Count { get; set; } = 1;
    }

    private List<InventoryItem> _inventory = new List<InventoryItem>();
    private int _selectedInventoryIndex = 0; // 0 to domyślnie pięść
    private bool _heldObjectFromInventory;

    private float _cameraPitch;
    private float _interactionHoldTime = 0.0f;
    private float _requiredInteractionTime = 1.0f;
    private bool _isHoldingInteraction = false;
    private float _interactionCooldownTimer = 0.0f;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;

        _camera = GetNode<Camera3D>("Camera3D");
        _interactRay = GetNode<RayCast3D>("Camera3D/InteractRay");

        _heldObjectController = GetNode<HeldObjectController>("HeldObjectController");
        _heldObjectController.ObjectPickedUp += OnObjectPickedUp;

        _punchingController = GetNode<PunchingController>("Punching");
        _punchingController.PunchHitFrame += TryPunchTarget;

        _interactionUiCanvas = GetNode<CanvasLayer>("InteractionUI");
        _interactionTextLabel = _interactionUiCanvas.GetNode<Label>("Container/VBoxContainer/Label");
        _interactionProgressBar = _interactionUiCanvas.GetNode<ProgressBar>("Container/VBoxContainer/InteractionProgressBar");

        _pickedUpObjectCanvas = GetNode<CanvasLayer>("PickedUpObjectInteractionUI");


        _inventoryUI = GetNodeOrNull<InventoryUI>("InventoryUI");

        _throwLabel = _pickedUpObjectCanvas.GetNode<Label>("Container/VBoxContainer/ThrowLabel");
        _takeLabel = _pickedUpObjectCanvas.GetNode<Label>("Container/VBoxContainer/TakeLabel");

        _interactionUiCanvas.Hide();
        _pickedUpObjectCanvas.Hide();
        if (_interactionProgressBar != null)
            _interactionProgressBar.Modulate = new Color(1, 1, 1, 0);

        UpdateInventoryUI();
    }

    private void OnObjectPickedUp(ThrowableObject body)
    {
        _heldObjectFromInventory = false;
        ObjectPickedUp?.Invoke(body);
        UpdatePickedUpUIState();
    }

    private void UpdatePickedUpUIState()
    {
        if (_heldObjectController.HeldObject == null)
        {
            _pickedUpObjectCanvas.Hide();
            return;
        }

        _pickedUpObjectCanvas.Show();

        bool canTake = _heldObjectController.HeldObject.IsEquippable && !_heldObjectFromInventory;

        if (_throwLabel != null) _throwLabel.Show();
        if (_takeLabel != null) _takeLabel.Visible = canTake;
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

        // Obsługa scrolla myszy do zmiany wybranego slotu
        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            if (mb.ButtonIndex == MouseButton.WheelUp)
            {
                ChangeInventorySelection(_selectedInventoryIndex - 1);
                GetViewport().SetInputAsHandled();
            }
            else if (mb.ButtonIndex == MouseButton.WheelDown)
            {
                ChangeInventorySelection(_selectedInventoryIndex + 1);
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Wybór slotu za pomocą klawiszy numerycznych 1-9
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.IsEcho())
        {
            if (keyEvent.Keycode >= Key.Key1 && keyEvent.Keycode <= Key.Key9)
            {
                int index = (int)keyEvent.Keycode - (int)Key.Key1;
                SelectSlot(index);
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        // Schowanie przedmiotu do plecaka (wymaga akcji "take_to_inventory" w Input Map, np. Q)
        if (@event.IsActionPressed("take_to_inventory") && !IsCinematicDialogueActive())
        {
            if (_heldObjectController.HeldObject != null
                && _heldObjectController.HeldObject.IsEquippable
                && !_heldObjectFromInventory)
            {
                GetViewport().SetInputAsHandled();
                StoreHeldObjectToInventory();
                return;
            }
        }

        // Lewy przycisk myszy (Atak pięścią lub Rzut / Użycie trzymanego przedmiotu)
        if (@event is InputEventMouseButton mouseButton
            && mouseButton.ButtonIndex == MouseButton.Left
            && mouseButton.Pressed
            && !mouseButton.IsEcho())
        {
            if (_selectedInventoryIndex == 0 && _heldObjectController.HeldObject == null && CanStartPunch())
            {
                if (_punchingController.StartPunch())
                    GetViewport().SetInputAsHandled();
            }
            else if (_heldObjectController.HeldObject != null && !IsCinematicDialogueActive())
            {
                GetViewport().SetInputAsHandled();
                _heldObjectController.ThrowObject(FixedThrowForce);

                bool stackRemaining = RemoveCurrentSelectedInventoryItem();

                ResetThrowUI();
                _interactionCooldownTimer = 0.3f;
                EquipNextFromStackOrFist(stackRemaining);
                return;
            }

            return;
        }

        // Upuszczenie przedmiotu
        if (@event.IsActionPressed("drop") && !IsCinematicDialogueActive())
        {
            if (_heldObjectController.HeldObject != null)
            {
                GetViewport().SetInputAsHandled();
                _heldObjectController.DropObject();
                bool stackRemaining = RemoveCurrentSelectedInventoryItem();
                ResetThrowUI();
                ResetHoldInteraction();
                _interactionCooldownTimer = 0.3f;
                EquipNextFromStackOrFist(stackRemaining);
                return;
            }
        }

        // Interakcja ze światem
        if (@event.IsActionPressed("interact") && !IsCinematicDialogueActive())
        {
            if (_heldObjectController.HeldObject == null
                && _currentInteractable != null
                && !_currentInteractable.RequiresHold)
            {
                GetViewport().SetInputAsHandled();
                TryInteract();
                return;
            }
        }
    }

    private void ChangeInventorySelection(int newIndex)
    {
        int totalSlotsCount = _inventory.Count + 1; // +1 dla slotu pięści
        newIndex = (newIndex + totalSlotsCount) % totalSlotsCount;
        SelectSlot(newIndex);
    }

    private void SelectSlot(int slotIndex)
    {
        // Nieekwipowalnego przedmiotu nie da się schować – najpierw trzeba go rzucić lub upuścić
        var held = _heldObjectController.HeldObject;
        if (held != null && !held.IsEquippable)
            return;

        if (slotIndex == _selectedInventoryIndex
            && (_heldObjectController.HeldObject == null || _heldObjectFromInventory))
        {
            UpdateInventoryUI();
            return;
        }

        if (slotIndex == 0)
        {
            if (_heldObjectController.HeldObject != null)
            {
                // Schowaj przedmiot z rąk do plecaka i przełącz na pięść
                StoreHeldObjectToInventoryQuietly();
            }
            _selectedInventoryIndex = 0;
        }
        else
        {
            int inventoryListIndex = slotIndex - 1;
            if (inventoryListIndex < _inventory.Count)
            {
                _selectedInventoryIndex = slotIndex;
                EquipItemFromInventory(inventoryListIndex);
            }
        }
        UpdateInventoryUI(showOnSelectionChange: true);
    }

    private void StoreHeldObjectToInventory()
    {
        var obj = _heldObjectController.HeldObject;
        if (obj == null || !obj.IsEquippable) return;

        if (!_heldObjectFromInventory)
            AddToCartInventory(obj);

        obj.QueueFree();
        _heldObjectController.ClearHeldObjectReference();
        _heldObjectFromInventory = false;

        ResetThrowUI();
        _selectedInventoryIndex = 0;
        UpdateInventoryUI(showOnSelectionChange: true);
    }

    private void StoreHeldObjectToInventoryQuietly()
    {
        var obj = _heldObjectController.HeldObject;
        if (obj != null)
        {
            if (!obj.IsEquippable)
                return;

            if (!_heldObjectFromInventory)
            {
                AddToCartInventory(obj);
            }
            obj.QueueFree();
            _heldObjectController.ClearHeldObjectReference();
        }
        _heldObjectFromInventory = false;
        ResetThrowUI();
    }

    private void AddToCartInventory(ThrowableObject obj)
    {
        string itemKey = !string.IsNullOrEmpty(obj.SceneFilePath) ? obj.SceneFilePath : obj.Name.ToString();
        var existing = _inventory.Find(i => i.ItemName == itemKey);
        if (existing != null)
        {
            existing.Count++;
        }
        else
        {
            PackedScene loadedScene = null;
            if (!string.IsNullOrEmpty(obj.SceneFilePath))
            {
                loadedScene = GD.Load<PackedScene>(obj.SceneFilePath);
            }

            _inventory.Add(new InventoryItem
            {
                ItemName = itemKey,
                ObjectScene = loadedScene, // Pobrane automatycznie!
                Icon = obj.Icon,
                Count = 1
            });
        }
    }

    private void EquipItemFromInventory(int index)
    {
        if (_heldObjectController.HeldObject != null)
        {
            StoreHeldObjectToInventoryQuietly();
        }

        var item = _inventory[index];

        if (item.ObjectScene != null)
        {
            var newObj = item.ObjectScene.Instantiate<ThrowableObject>();
            GetTree().CurrentScene.AddChild(newObj);
            if (_heldObjectController.TryPickup(newObj))
                _heldObjectFromInventory = true;
            else
                newObj.QueueFree();
        }

        UpdatePickedUpUIState();
    }

    /// <returns>True, jeśli w wybranym slocie zostały jeszcze sztuki przedmiotu.</returns>
    private bool RemoveCurrentSelectedInventoryItem()
    {
        if (_heldObjectFromInventory && _selectedInventoryIndex > 0)
        {
            _heldObjectFromInventory = false;
            int listIdx = _selectedInventoryIndex - 1;
            if (listIdx < _inventory.Count)
            {
                _inventory[listIdx].Count--;
                if (_inventory[listIdx].Count <= 0)
                {
                    _inventory.RemoveAt(listIdx);
                    return false;
                }
                return true;
            }
        }
        return false;
    }

    private void EquipNextFromStackOrFist(bool stackRemaining)
    {
        if (stackRemaining)
        {
            EquipItemFromInventory(_selectedInventoryIndex - 1);
            UpdateInventoryUI(showOnSelectionChange: true);
        }
        else
        {
            SelectSlot(0);
        }
    }

    private void UpdateInventoryUI(bool showOnSelectionChange = false)
    {
        if (_inventoryUI != null)
        {
            _inventoryUI.RefreshInventory(_inventory, _selectedInventoryIndex, showOnSelectionChange);
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

        if (_interactionCooldownTimer > 0.0f)
        {
            _interactionCooldownTimer -= (float)delta;
        }

        if (!IsCinematicDialogueActive())
        {
            UpdateInteractionTarget();
        }

        HandleHoldInteraction((float)delta);
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

        if (_interactionProgressBar != null && _heldObjectController.HeldObject == null)
        {
            _interactionProgressBar.Value = 0;
            _interactionProgressBar.Modulate = new Color(1, 1, 1, 0);
        }
    }

    private void ResetThrowUI()
    {
        if (_interactionProgressBar != null)
        {
            _interactionProgressBar.Value = 0;
            _interactionProgressBar.Modulate = new Color(1, 1, 1, 0);
        }
        _interactionUiCanvas.Hide();
        _pickedUpObjectCanvas.Hide();
    }

    private bool IsCinematicDialogueActive()
    {
        var controller = GetTree().CurrentScene
            ?.GetNodeOrNull<CutsceneController>("CutsceneController");

        return controller?.IsCinematicDialogueActive == true;
    }

    private void UpdateInteractionTarget()
    {
        if (_interactionCooldownTimer > 0.0f)
        {
            _interactionUiCanvas.Hide();
            return;
        }

        if (_heldObjectController.HeldObject != null)
        {
            if (MovementLocked || IsCinematicDialogueActive())
            {
                _interactionUiCanvas.Hide();
                return;
            }

            if (_interactionProgressBar != null)
                _interactionProgressBar.Modulate = new Color(1, 1, 1, 0);

            UpdatePickedUpUIState();
            return;
        }

        IInteractable nextTarget = null;

        if (!MovementLocked
            && !IsCinematicDialogueActive()
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
        _interactionTextLabel.Show();

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

        return _heldObjectController.HeldObject == null && _heldObjectController.TryPickup(body);
    }

    private void PushRigidBodies()
    {
        for (var i = 0; i < GetSlideCollisionCount(); i++)
        {
            var collision = GetSlideCollision(i);

            if (collision.GetCollider() is RigidBody3D rb)
            {
                var pushDirection = -collision.GetNormal();
                pushDirection.Y = 0;

                if (pushDirection.LengthSquared() == 0)
                    continue;

                pushDirection = pushDirection.Normalized();
                rb.ApplyCentralImpulse(pushDirection * 2.0f);
            }
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
        if (MovementLocked || _heldObjectController.HeldObject != null)
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
