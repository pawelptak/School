using Godot;
using System;

public partial class Mate : InteractableCharacter
{
	private DialogueUI _dialogueUI;

	private bool _waitingForDoorObjective;

	public event Action GoToDoor;

	public override void _Ready()
	{
		base._Ready();

		_dialogueUI = GetTree().CurrentScene.GetNode<DialogueUI>("DialogueUI");

		HitByThrowable += OnHitByThrowable;
		_dialogueUI.DialogueFinished += OnDialogueFinished;
	}

	private void OnHitByThrowable(float throwForce)
	{
		if (throwForce < 15.0f)
		{
			_dialogueUI.ShowDialogue(new DialogueLine[]
			{
				new DialogueLine(
					DialogueSpeaker.Mate,
					string.Format(TutorialText.MateThrowAgain, "LPM")
				)
			});

			return;
		}

		_waitingForDoorObjective = true;

		_dialogueUI.ShowDialogue(new DialogueLine[]
		{
			new DialogueLine(
				DialogueSpeaker.Mate,
				TutorialText.MateThrowGood
			),
			new DialogueLine(
				DialogueSpeaker.Mate,
				TutorialText.MateGoToDoor
			)
		});
	}

	private void OnDialogueFinished()
	{
		if (!_waitingForDoorObjective)
			return;

		_waitingForDoorObjective = false;

		GoToDoor?.Invoke();
	}
}
