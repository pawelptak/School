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
	/// <summary>Immediate throw force, also the maximum force when charging is enabled.</summary>
	[Export] public float FixedThrowForce { get; set; } = 20.0f;
	/// <summary>Hold LMB to charge and release to throw. Disabled by default.</summary>
	[Export] public bool ChargeThrowEnabled { get; set; } = false;
	/// <summary>Force of a quick click when charging is enabled.</summary>
	[Export(PropertyHint.Range, "0,100,0.1,or_greater")]
	public float MinThrowForce { get; set; } = 5.0f;
	/// <summary>Seconds required to reach FixedThrowForce.</summary>
	[Export(PropertyHint.Range, "0.1,10,0.1,or_greater")]
	public float ThrowChargeDuration { get; set; } = 1.5f;

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
	private Label _interactableNameLabel;
	private ProgressBar _interactionProgressBar;

	private Label _throwLabel;
	private Label _takeLabel;

	private HeldObjectController _heldObjectController;
	private PunchingController _punchingController;

	public class InventoryItem
	{
		public string ItemName { get; set; }
		public PackedScene ObjectScene { get; set; }
		public Texture2D Icon { get; set; }
		public int Count { get; set; } = 1;
	}

	private List<InventoryItem> _inventory = new List<InventoryItem>();
	private int _selectedInventoryIndex = 0;
	private bool _heldObjectFromInventory;
	private ThrowableObject _chargingThrowObject;
	private float _throwChargeTime;

	private float _cameraPitch;
	private float _interactionHoldTime = 0.0f;
	private float _requiredInteractionTime = 1.0f;
	private bool _isHoldingInteraction = false;
	private float _interactionCooldownTimer = 0.0f;

	public bool IsBlocking => _blockingController?.IsBlocking == true;

	private BlockingController _blockingController;
	private bool _blockButtonHeld;


	[ExportGroup("Blocking")]
	[Export(PropertyHint.Range, "0,1,0.05")]
	public float BlockDamageMultiplier { get; set; } = 0.3f;
	[Export] public float PerfectBlockWindow { get; set; } = 0.2f;
	[Export] public float PerfectBlockStunDuration { get; set; } = 1.2f;
	[Export] public bool PerfectBlockEnabled { get; set; } = true;
	private float _perfectBlockTimer;

	public float CalculateIncomingDamage(float damage)
	{
		if (!_blockButtonHeld)
			return damage;

		return damage * Mathf.Clamp(BlockDamageMultiplier, 0.0f, 1.0f);
	}

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;

		_camera = GetNode<Camera3D>("Camera3D");
		_interactRay = GetNode<RayCast3D>("Camera3D/InteractRay");

		_heldObjectController = GetNode<HeldObjectController>("HeldObjectController");
		_heldObjectController.ObjectPickedUp += OnObjectPickedUp;

		_punchingController = GetNode<PunchingController>("Punching");
		_punchingController.PunchHitFrame += TryPunchTarget;

		_blockingController = GetNode<BlockingController>("Blocking");

		_interactionUiCanvas = GetNode<CanvasLayer>("InteractionUI");
		_interactionTextLabel = _interactionUiCanvas.GetNode<Label>("Container/VBoxContainer/Label");
		_interactableNameLabel = _interactionUiCanvas.GetNode<Label>("Container/VBoxContainer/InteractableName");
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
		ResetThrowCharge();
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
		var canTake = _heldObjectController.HeldObject.IsEquippable && !_heldObjectFromInventory;

		if (_throwLabel != null) _throwLabel.Show();
		if (_takeLabel != null) _takeLabel.Visible = canTake;
	}

	public override void _Input(InputEvent @event)
	{
		if (CameraLocked) return;

		if (@event is InputEventMouseMotion mouseMotion)
		{
			RotateY(-mouseMotion.Relative.X * 0.01f);
			_cameraPitch -= mouseMotion.Relative.Y * 0.01f;
			_cameraPitch = Mathf.Clamp(_cameraPitch, Mathf.DegToRad(-80), Mathf.DegToRad(80));
			_camera.Rotation = new Vector3(_cameraPitch, 0, 0);
		}

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

	public bool TryPerfectBlock(NpcController attacker)
	{
		if (!PerfectBlockEnabled || _perfectBlockTimer <= 0.0f || !_blockButtonHeld)
			return false;

		_perfectBlockTimer = 0.0f;

		if (attacker != null)
		{
			attacker.ApplyKnockback(
				Vector3.Zero,
				0.0f,
				PerfectBlockStunDuration
			);
		}

		GD.Print("[COMBAT] Perfect block!");

		return true;
	}

	public void ReactToBlockedHit()
	{
		if (_blockButtonHeld)
		{
			_blockingController?.PlayHitReaction();
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton rightMouseButton
			&& rightMouseButton.ButtonIndex == MouseButton.Right)
		{
			if (!rightMouseButton.Pressed)
			{
				if (_blockButtonHeld)
				{
					_blockButtonHeld = false;
					_perfectBlockTimer = 0.0f;
					_blockingController.StopBlocking();
					GetViewport().SetInputAsHandled();
				}

				return;
			}

			if (rightMouseButton.IsEcho())
				return;

			if (!GameManager.Instance.CurrentSaveData.BlockingUnlocked
				|| MovementLocked
				|| CameraLocked
				|| IsCinematicDialogueActive()
				|| _heldObjectController.HeldObject != null
				)
			{
				return;
			}

			_blockButtonHeld = true;
			_perfectBlockTimer = PerfectBlockWindow;

			if (_punchingController.IsPlaying)
			{
				_punchingController.CancelPunch();
			}

			_blockingController.StartBlocking();
			GetViewport().SetInputAsHandled();
			return;
		}


		if (@event.IsActionPressed("toggle_grade_journal"))
		{
			if (!GameManager.Instance.CurrentSaveData.GradeJournalUnlocked)
				return;

			var journal = GetNodeOrNull<GradeJournal>("/root/GradeJournal");

			journal?.ToggleJournal();

			MovementLocked = journal.Visible;
			CameraLocked = journal.Visible;

			GetViewport().SetInputAsHandled();
		}

		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.IsEcho())
		{
			if (keyEvent.Keycode >= Key.Key1 && keyEvent.Keycode <= Key.Key9)
			{
				var index = (int)keyEvent.Keycode - (int)Key.Key1;
				SelectSlot(index);
				GetViewport().SetInputAsHandled();
				return;
			}
		}

		if (@event.IsActionPressed("take_to_inventory") && !IsCinematicDialogueActive())
		{
			if (_heldObjectController.HeldObject != null && _heldObjectController.HeldObject.IsEquippable && !_heldObjectFromInventory)
			{
				GetViewport().SetInputAsHandled();
				StoreHeldObjectToInventory();
				return;
			}
		}

		if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left && !mouseButton.IsEcho())
		{
			if (!mouseButton.Pressed)
			{
				if (_chargingThrowObject != null)
				{
					GetViewport().SetInputAsHandled();
					if (IsThrowChargeValid())
					{
						var maxForce = Mathf.Max(0.0f, FixedThrowForce);
						var minForce = Mathf.Clamp(MinThrowForce, 0.0f, maxForce);
						ThrowHeldObject(Mathf.Lerp(minForce, maxForce, GetThrowChargeProgress()));
					}
					else
					{
						ResetThrowCharge();
					}
				}
				return;
			}

			if (_selectedInventoryIndex == 0 && _heldObjectController.HeldObject == null && CanStartPunch())
			{
				if (_punchingController.StartPunch())
					GetViewport().SetInputAsHandled();
			}
			else if (_heldObjectController.HeldObject != null && !IsCinematicDialogueActive())
			{
				GetViewport().SetInputAsHandled();
				if (ChargeThrowEnabled)
				{
					if (!MovementLocked && !CameraLocked)
					{
						_chargingThrowObject = _heldObjectController.HeldObject;
						_throwChargeTime = 0.0f;
						UpdatePickedUpUIState();
					}
				}
				else
				{
					ThrowHeldObject(FixedThrowForce);
				}
				return;
			}
			return;
		}

		if (@event.IsActionPressed("drop") && !IsCinematicDialogueActive())
		{
			var heldObject = _heldObjectController.HeldObject;

			if (heldObject != null)
			{
				GetViewport().SetInputAsHandled();

				heldObject.DroppedFromInventory = true;

				var stackRemaining = RemoveCurrentSelectedInventoryItem();

				_heldObjectController.DropObject();

				ResetThrowUI();
				ResetHoldInteraction();
				_interactionCooldownTimer = 0.3f;

				EquipNextFromStackOrFist(stackRemaining);
				return;
			}
		}

		if (@event.IsActionPressed("interact") && !IsCinematicDialogueActive())
		{
			if (_heldObjectController.HeldObject == null && _currentInteractable != null && !_currentInteractable.RequiresHold)
			{
				GetViewport().SetInputAsHandled();
				TryInteract();
				return;
			}
		}
	}

	private void ChangeInventorySelection(int newIndex)
	{
		var totalSlotsCount = _inventory.Count + 1;
		newIndex = (newIndex + totalSlotsCount) % totalSlotsCount;
		SelectSlot(newIndex);
	}

	private void SelectSlot(int slotIndex)
	{
		var held = _heldObjectController.HeldObject;
		if (held != null && !held.IsEquippable) return;

		if (slotIndex == _selectedInventoryIndex && (_heldObjectController.HeldObject == null || _heldObjectFromInventory))
		{
			UpdateInventoryUI();
			return;
		}

		if (slotIndex == 0)
		{
			if (_heldObjectController.HeldObject != null) StoreHeldObjectToInventoryQuietly();
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

		if (!_heldObjectFromInventory) AddToCartInventory(obj);

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
			if (!obj.IsEquippable) return;
			if (!_heldObjectFromInventory) AddToCartInventory(obj);
			obj.QueueFree();
			_heldObjectController.ClearHeldObjectReference();
		}
		_heldObjectFromInventory = false;
		ResetThrowUI();
	}

	private void AddToCartInventory(ThrowableObject obj)
	{
		var itemKey = !string.IsNullOrEmpty(obj.SceneFilePath) ? obj.SceneFilePath : obj.Name.ToString();
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
				ObjectScene = loadedScene,
				Icon = obj.Icon,
				Count = 1
			});

			RegisterWorldItemAsCollected(obj);
		}
	}

	private void EquipItemFromInventory(int index)
	{
		if (_heldObjectController.HeldObject != null) StoreHeldObjectToInventoryQuietly();

		var item = _inventory[index];
		if (item.ObjectScene != null)
		{
			var newObj = item.ObjectScene.Instantiate<ThrowableObject>();
			GetTree().CurrentScene.AddChild(newObj);
			if (_heldObjectController.TryPickup(newObj)) _heldObjectFromInventory = true;
			else newObj.QueueFree();
		}
		UpdatePickedUpUIState();
	}

	private bool RemoveCurrentSelectedInventoryItem()
	{
		if (_heldObjectFromInventory && _selectedInventoryIndex > 0)
		{
			_heldObjectFromInventory = false;
			var listIdx = _selectedInventoryIndex - 1;
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

		var velocity = Velocity;
		if (!IsOnFloor()) velocity += GetGravity() * (float)delta;
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor()) velocity.Y = JumpVelocity;

		var inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
		var direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

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
		UpdateThrowCharge((float)delta);

		if (_interactionCooldownTimer > 0.0f) _interactionCooldownTimer -= (float)delta;
		if (!IsCinematicDialogueActive()) UpdateInteractionTarget();

		HandleHoldInteraction((float)delta);

		if (_perfectBlockTimer > 0.0f)
		{
			_perfectBlockTimer = Mathf.Max(
				0.0f,
				_perfectBlockTimer - (float)delta
			);
		}
	}

	private void ThrowHeldObject(float force)
	{
		if (_heldObjectController.HeldObject != null)
		{
			_heldObjectController.HeldObject.DroppedFromInventory = true;
		}

		_heldObjectController.ThrowObject(force);
		var stackRemaining = RemoveCurrentSelectedInventoryItem();
		ResetThrowUI();
		_interactionCooldownTimer = 0.3f;
		EquipNextFromStackOrFist(stackRemaining);
	}

	private bool IsThrowChargeValid()
	{
		return ChargeThrowEnabled
			&& _chargingThrowObject != null
			&& IsInstanceValid(_chargingThrowObject)
			&& ReferenceEquals(_chargingThrowObject, _heldObjectController.HeldObject)
			&& !MovementLocked && !CameraLocked && !IsCinematicDialogueActive();
	}

	private float GetThrowChargeProgress()
	{
		return Mathf.Clamp(_throwChargeTime / Mathf.Max(0.1f, ThrowChargeDuration), 0.0f, 1.0f);
	}

	private void UpdateThrowCharge(float delta)
	{
		if (_chargingThrowObject == null) return;
		if (!IsThrowChargeValid() || !Input.IsMouseButtonPressed(MouseButton.Left))
		{
			ResetThrowCharge();
			return;
		}
		_throwChargeTime = Mathf.Min(_throwChargeTime + delta, Mathf.Max(0.1f, ThrowChargeDuration));
	}

	private void ResetThrowCharge()
	{
		if (_chargingThrowObject != null)
		{
			if (_interactionProgressBar != null)
			{
				_interactionProgressBar.Value = 0;
				_interactionProgressBar.Modulate = new Color(1, 1, 1, 0);
			}
			_interactionUiCanvas.Hide();
		}
		_chargingThrowObject = null;
		_throwChargeTime = 0.0f;
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
				var progress = Mathf.Clamp(_interactionHoldTime / _requiredInteractionTime, 0.0f, 1.0f);

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
		ResetThrowCharge();
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
		var controller = GetTree().CurrentScene?.GetNodeOrNull<CutsceneController>("CutsceneController");
		return controller?.IsCinematicDialogueActive == true;
	}

	private void UpdateInteractionTarget()
	{
		if (_interactionCooldownTimer > 0.0f && _chargingThrowObject == null)
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

			bool isChargingThrow = _chargingThrowObject != null;
			_interactionTextLabel.Hide();
			_interactableNameLabel.Hide();
			_interactionUiCanvas.Visible = isChargingThrow;

			if (_interactionProgressBar != null)
			{
				_interactionProgressBar.ShowPercentage = false;
				_interactionProgressBar.Modulate = new Color(1, 1, 1, isChargingThrow ? 1 : 0);
				_interactionProgressBar.Value = isChargingThrow ? GetThrowChargeProgress() * 100.0f : 0;
			}

			UpdatePickedUpUIState();
			return;
		}

		IInteractable nextTarget = null;
		if (!MovementLocked && !IsCinematicDialogueActive() && _interactRay.IsColliding())
		{
			var interactable = FindInteractable(_interactRay.GetCollider() as Node);
			if (interactable != null && interactable.CanInteract(this))
				nextTarget = interactable;
		}

		if (ReferenceEquals(nextTarget, _currentInteractable)) return;

		ResetHoldInteraction();
		_currentInteractable = nextTarget;

		if (_currentInteractable == null)
		{
			_interactionUiCanvas.Hide();
			return;
		}

		_interactionTextLabel.Text = _currentInteractable.GetPromptText();
		_interactableNameLabel.Text = _currentInteractable.Name;
		_interactionTextLabel.Show();
		_interactableNameLabel.Show();

		if (_interactionProgressBar != null)
			_interactionProgressBar.Modulate = new Color(1, 1, 1, 0);

		_interactionUiCanvas.Show();
	}

	private void TryInteract()
	{
		if (_currentInteractable == null) return;
		var interactable = _currentInteractable;
		if (!interactable.CanInteract(this)) return;

		_currentInteractable = null;
		_interactionUiCanvas.Hide();
		interactable.Interact(this);
	}

	private static IInteractable FindInteractable(Node node)
	{
		for (Node current = node; current != null; current = current.GetParent())
		{
			if (current is IInteractable interactable) return interactable;
		}
		return null;
	}

	public bool TryPickup(ThrowableObject body)
	{
		if (MovementLocked || IsCinematicDialogueActive()) return false;
		return _heldObjectController.HeldObject == null && _heldObjectController.TryPickup(body);
	}

	private void PushRigidBodies()
	{
		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			var collision = GetSlideCollision(i);
			if (collision.GetCollider() is RigidBody3D rb)
			{
				var pushDirection = -collision.GetNormal();
				pushDirection.Y = 0;
				if (pushDirection.LengthSquared() == 0) continue;
				pushDirection = pushDirection.Normalized();
				rb.ApplyCentralImpulse(pushDirection * 2.0f);
			}
		}
	}

	private bool CanStartPunch()
	{
		var isAttackUnlocked = GameManager.Instance != null && GameManager.Instance.CurrentSaveData.AttackingUnlocked;

		if (!isAttackUnlocked
			|| MovementLocked
			|| _punchingController.IsPlaying
			|| _blockingController.IsBlocking
			|| _blockingController.IsPlaying)
		{
			return false;
		}

		return !IsCinematicDialogueActive();
	}

	private void TryPunchTarget()
	{
		if (MovementLocked || _heldObjectController.HeldObject != null || IsCinematicDialogueActive())
			return;

		var origin = _camera.GlobalPosition;
		var direction = -_camera.GlobalTransform.Basis.Z;
		var end = origin + (direction * AttackDistance);

		var query = PhysicsRayQueryParameters3D.Create(origin, end);
		query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
		query.CollideWithAreas = false;

		var result = GetWorld3D().DirectSpaceState.IntersectRay(query);
		if (result.Count == 0) return;

		var collider = result["collider"].AsGodotObject() as Node;
		if (collider == null) return;

		var interactable = FindInteractable(collider);
		if (interactable is not InteractableCharacter character) return;

		character.NotifyHitByPunch(this, AttackDamage, AttackKnockback);
	}

	public override void _ExitTree()
	{
		if (_heldObjectController != null && GodotObject.IsInstanceValid(_heldObjectController))
		{
			_heldObjectController.ObjectPickedUp -= OnObjectPickedUp;
		}

		if (_punchingController != null && GodotObject.IsInstanceValid(_punchingController))
		{
			_punchingController.PunchHitFrame -= TryPunchTarget;
		}
	}

	public void SaveToData(PlayerSaveData saveData)
	{
		saveData.InventoryItems.Clear();

		foreach (InventoryItem item in _inventory)
		{
			saveData.InventoryItems.Add(new InventoryItemSaveData
			{
				ItemName = item.ItemName,
				SceneFilePath = item.ObjectScene?.ResourcePath ?? "",
				Count = item.Count
			});
		}
	}

	public void LoadFromData(PlayerSaveData saveData)
	{
		_inventory.Clear();

		foreach (InventoryItemSaveData savedItem in saveData.InventoryItems)
		{
			PackedScene loadedScene = null;
			if (!string.IsNullOrEmpty(savedItem.SceneFilePath))
			{
				loadedScene = GD.Load<PackedScene>(savedItem.SceneFilePath);
			}

			Texture2D icon = null;
			if (loadedScene != null)
			{
				var tempObj = loadedScene.Instantiate<ThrowableObject>();
				icon = tempObj.Icon;
				tempObj.QueueFree();
			}

			_inventory.Add(new InventoryItem
			{
				ItemName = savedItem.ItemName,
				ObjectScene = loadedScene,
				Icon = icon,
				Count = savedItem.Count
			});
		}

		_selectedInventoryIndex = 0;
		UpdateInventoryUI();
	}

	private void RegisterWorldItemAsCollected(ThrowableObject obj)
	{
		if (obj != null && GameManager.Instance != null)
		{
			string worldId = obj.WorldItemID;

			if (!GameManager.Instance.CurrentSaveData.CollectedWorldItems.Contains(worldId))
			{
				GameManager.Instance.CurrentSaveData.CollectedWorldItems.Add(worldId);
			}
		}
	}

	public bool IsAttackingFromFront(Node3D attacker)
	{
		if (attacker == null || !GodotObject.IsInstanceValid(attacker))
			return false;

		Vector3 toAttacker = attacker.GlobalPosition - GlobalPosition;

		toAttacker.Y = 0.0f;

		if (toAttacker.LengthSquared() < 0.001f)
			return true;

		toAttacker = toAttacker.Normalized();

		Vector3 forward = -GlobalTransform.Basis.Z;
		forward.Y = 0.0f;
		forward = forward.Normalized();

		return forward.Dot(toAttacker) > 0.0f;
	}
}
