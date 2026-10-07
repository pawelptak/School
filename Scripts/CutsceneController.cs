using Godot;
using System;

public partial class CutsceneController : Node
{
	private Player _player;
	private DialogueUI _dialogueUI;

	private Vector3 _originalCameraPosition;
	private Vector3 _originalCameraRotation;

	private bool _cinematicDialogueActive;

	public event Action CinematicDialogueFinished;

	public override void _Ready()
	{
		_player = GetTree().CurrentScene.GetNode<Player>("Player");
		_dialogueUI = GetTree().CurrentScene.GetNode<DialogueUI>("DialogueUI");

		_dialogueUI.DialogueFinished += OnDialogueFinished;
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("interact") && _dialogueUI.IsVisible())
		{
			_dialogueUI.NextMessage();
		}
	}

	public void StartDialogue(
		Node3D character,
		string[] messages,
		bool cinematic = false
	)
	{
		_cinematicDialogueActive = cinematic;

		_dialogueUI.ShowDialogue(messages);

		if (!cinematic)
			return;

		_player.MovementLocked = true;
		_player.CameraLocked = true;

		var camera = _player.GetNode<Camera3D>("Camera3D");

		_originalCameraPosition = camera.GlobalPosition;
		_originalCameraRotation = camera.GlobalRotation;

		var lookAtPoint =
			character.GetNodeOrNull<Marker3D>("DialogueLookAtPoint");

		if (lookAtPoint == null)
		{
			GD.PrintErr(
				$"Nie znaleziono DialogueLookAtPoint w {character.Name}!"
			);

			return;
		}

		var directionFromCharacterToPlayer =
			_player.GlobalPosition - character.GlobalPosition;

		directionFromCharacterToPlayer.Y = 0;

		if (directionFromCharacterToPlayer.LengthSquared() == 0)
		{
			GD.PrintErr("Gracz i rozmówca są w tej samej pozycji.");
			return;
		}

		directionFromCharacterToPlayer =
			directionFromCharacterToPlayer.Normalized();

		var targetPosition =
			character.GlobalPosition
			+ directionFromCharacterToPlayer * 0.9f;

		targetPosition.Y = lookAtPoint.GlobalPosition.Y;

		var targetRotation = camera.GlobalTransform
			.LookingAt(
				lookAtPoint.GlobalPosition,
				Vector3.Up
			)
			.Basis
			.GetEuler();

		var tween = CreateTween();

		tween.SetParallel();

		tween.TweenProperty(
			camera,
			"global_position",
			targetPosition,
			0.4
		);

		tween.TweenProperty(
			camera,
			"global_rotation",
			targetRotation,
			0.4
		);
	}

	private void OnDialogueFinished()
	{
		if (!_cinematicDialogueActive)
			return;

		_cinematicDialogueActive = false;

		var camera = _player.GetNode<Camera3D>("Camera3D");

		var tween = CreateTween();

		tween.SetParallel();

		tween.TweenProperty(
			camera,
			"global_position",
			_originalCameraPosition,
			0.4
		);

		tween.TweenProperty(
			camera,
			"global_rotation",
			_originalCameraRotation,
			0.4
		);

		tween.SetParallel(false);

		tween.TweenCallback(
			Callable.From(() =>
			{
				_player.MovementLocked = false;
				_player.CameraLocked = false;

				CinematicDialogueFinished?.Invoke();
			})
		);
	}
}
