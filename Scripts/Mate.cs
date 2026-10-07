using Godot;

public partial class Mate : InteractableCharacter
{
	private DialogueUI _dialogueUI;

	public override void _Ready()
	{
		base._Ready();

		_dialogueUI = GetTree().CurrentScene.GetNode<DialogueUI>("DialogueUI");
	}

	protected override void OnInteraction()
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
