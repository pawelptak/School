using Godot;

public partial class ClassroomTutorial : Node3D
{
	private Mate _mate;
	private CutsceneController _cutsceneController;

	public override void _Ready()
	{
		_mate = GetNode<Mate>("Mate");
		_cutsceneController = GetNode<CutsceneController>("CutsceneController");

		_cutsceneController.CinematicDialogueFinished += OnCinematicDialogueFinished;

		_cutsceneController.StartDialogue(
			_mate,
			new string[]
			{
				GameText.MateWakeUp,
				GameText.MateLessonEnding,
				GameText.MateBreakEnding,
				GameText.MateWakeUpInstruction
			},
			true
		);
	}

	private void OnCinematicDialogueFinished()
	{
		_mate.IntroFinished = true;
	}
}
