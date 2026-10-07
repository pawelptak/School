using Godot;

public partial class ClassroomTutorial : Node3D
{
	private Mate _mate;
	private DialogueUI _dialogueUI;

	public override void _Ready()
	{
		_mate = GetNode<Mate>("Mate");
		_dialogueUI = GetNode<DialogueUI>("DialogueUI");

		_mate.HitByThrowable += OnMateHitByThrowable;
	}

	private void OnMateHitByThrowable(float throwForce)
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

	private string GetThrowButtonName()
	{
		var events = InputMap.ActionGetEvents("throw");

		if (events.Count == 0)
			return "LMB";

		return events[0].AsText().Replace(" (Physical)", "");
	}
}
