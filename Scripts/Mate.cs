using Godot;
using System;

public partial class Mate : CharacterBody3D
{
	public const float Speed = 5.0f;
	public const float JumpVelocity = 4.5f;
	
	private bool _playerInRange;
	private bool _dialogueFinished;
	private DialogueUI _dialogueUI;

	public override void _Ready()
	{
		var interactionArea = GetNode<Area3D>("InteractionArea");
		interactionArea.BodyEntered += OnBodyEntered;
		interactionArea.BodyExited += OnBodyExited;
		
		_dialogueUI = GetTree().CurrentScene.GetNode<DialogueUI>("DialogueUI");
	}
	
	public override void _Process(double delta)
{
	if (_playerInRange && Input.IsActionJustPressed("interact"))
	{
			if (_dialogueUI.IsVisible())
			{
				_dialogueUI.NextMessage();
			}
			else
			{
				_dialogueUI.ShowDialogue(new string[]
				{
					GameText.MateWakeUp,
					GameText.MateLessonEnding,
					GameText.MateBreakEnding,
					GameText.MateWakeUpInstruction
				});
			}
	}
}

	public void OnHitByThrowable(float throwForce)
	{
		if (throwForce < 20.0f)
		{
			var throwButton = GetThrowButtonName();

			_dialogueUI.ShowDialogue(new string[]
			{
				string.Format(GameText.MateThrowAgain, throwButton)
			});
		}
		else
		{
			_dialogueUI.ShowDialogue(new string[]
			{
				GameText.MateThrowGood
			});
		}
	}
	
	private void OnBodyEntered(Node3D body)
	{
		if (body is Player)
		{
			_playerInRange = true;
		}
	}
	
	private void OnBodyExited(Node3D body)
	{
		if (body is Player)
		{
			_playerInRange = false;
		}
	}
	
	private string GetThrowButtonName()
	{
		var events = InputMap.ActionGetEvents("throw");

		if (events.Count == 0)
			return "LMB";

		return events[0].AsText().Replace(" (Physical)", "");
	}
}
