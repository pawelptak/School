using Godot;

public partial class Mate : InteractableCharacter
{
	private DialogueUI _dialogueUI;

	public bool IntroFinished { get; set; }

	public override void _Ready()
	{
		base._Ready();

		_dialogueUI = GetTree().CurrentScene.GetNode<DialogueUI>("DialogueUI");

		HitByThrowable += OnHitByThrowable;
	}

	protected override void OnInteraction()
	{
		if (!IntroFinished)
			return;
	}

	private void OnHitByThrowable(float throwForce)
	{
		if (throwForce < 15.0f)
		{
			_dialogueUI.ShowDialogue(new string[]
			{
				string.Format(GameText.MateThrowAgain, "LPM")
			});

			return;
		}

		_dialogueUI.ShowDialogue(new string[]
		{
			GameText.MateThrowGood,
			GameText.MateGoToDoor
		});
	}
}
