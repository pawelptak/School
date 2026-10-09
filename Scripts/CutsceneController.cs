using Godot;
using System;

public partial class CutsceneController : Node
{
	private Camera3D _camera;
	private Player _player;
	private DialogueUI _dialogueUI;

	private Vector3 _originalCameraPosition;
	private Vector3 _originalCameraRotation;

	private bool _cinematicDialogueActive;

	public bool IsDialogueVisible => _dialogueUI.Visible;

	public event Action DialogueFinished;
	public event Action CinematicDialogueFinished;

	public override void _Ready()
	{
		_dialogueUI = GetNode<DialogueUI>("DialogueUI");
		_dialogueUI.DialogueFinished += OnDialogueFinished;
	}

	public void SetCamera(Camera3D camera)
	{
		_camera = camera;
	}

	public void SetPlayer(Player player)
	{
		_player = player;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_dialogueUI == null)
			return;

		if (!@event.IsActionPressed("interact"))
			return;

		if (!_dialogueUI.IsVisible())
			return;

		GetViewport().SetInputAsHandled();

		_dialogueUI.NextMessage();
	}

	public void StartDialogue(
		Node3D character,
		DialogueLine[] messages,
		bool cinematic = false,
		Action onFinished = null)
	{
		_dialogueUI.ShowDialogue(messages, onFinished);

		if (!cinematic)
			return;

		_cinematicDialogueActive = true;

		if (_player != null)
		{
			_player.MovementLocked = true;
			_player.CameraLocked = true;
		}

		_originalCameraPosition = _camera.GlobalPosition;
		_originalCameraRotation = _camera.GlobalRotation;

		var lookAtPoint =
			character.GetNodeOrNull<Marker3D>("DialogueLookAtPoint");

		if (lookAtPoint == null)
		{
			GD.PrintErr(
				$"Nie znaleziono DialogueLookAtPoint w {character.Name}!"
			);

			return;
		}

		var directionFromCharacterToCamera =
			_camera.GlobalPosition - character.GlobalPosition;

		directionFromCharacterToCamera.Y = 0;

		if (directionFromCharacterToCamera.LengthSquared() == 0)
		{
			GD.PrintErr(
                "Kamera i rozmówca są w tej samej pozycji."
			);

			return;
		}

		directionFromCharacterToCamera =
			directionFromCharacterToCamera.Normalized();

		var targetPosition =
			character.GlobalPosition
			+ directionFromCharacterToCamera * 0.9f;

		targetPosition.Y = lookAtPoint.GlobalPosition.Y;

		var targetRotation = _camera.GlobalTransform
			.LookingAt(
				lookAtPoint.GlobalPosition,
				Vector3.Up
			)
			.Basis
			.GetEuler();

		var tween = CreateTween();

		tween.SetParallel();

		tween.TweenProperty(
			_camera,
			"global_position",
			targetPosition,
			0.4
		);

		tween.TweenProperty(
			_camera,
			"global_rotation",
			targetRotation,
			0.4
		);
	}

	private void OnDialogueFinished()
	{
		DialogueFinished?.Invoke();

		if (!_cinematicDialogueActive)
			return;

		_cinematicDialogueActive = false;

		var tween = CreateTween();

		tween.SetParallel();

		tween.TweenProperty(
			_camera,
			"global_position",
			_originalCameraPosition,
			0.4
		);

		tween.TweenProperty(
			_camera,
			"global_rotation",
			_originalCameraRotation,
			0.4
		);

		tween.SetParallel(false);

		tween.TweenCallback(
			Callable.From(() =>
			{
				if (_player != null)
				{
					_player.MovementLocked = false;
					_player.CameraLocked = false;
				}

				CinematicDialogueFinished?.Invoke();
			})
		);
	}
}
