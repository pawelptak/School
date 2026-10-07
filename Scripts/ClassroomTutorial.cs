using Godot;

public partial class ClassroomTutorial : Node3D
{
    private Mate _mate;
    private Player _player;
    private CutsceneController _cutsceneController;
    private Node3D _objectiveMarker;
    private ThrowableObject _backpack;

    public override void _Ready()
    {
        _mate = GetNode<Mate>("Mate");
        _player = GetNode<Player>("Player");
        _cutsceneController = GetNode<CutsceneController>("CutsceneController");
        _objectiveMarker = GetNode<Node3D>("ObjectiveMarker");
        _backpack = GetNode<ThrowableObject>("Backpack");

        _objectiveMarker.Hide();

        _player.ObjectPickedUp += OnObjectPickedUp;

        _cutsceneController.CinematicDialogueFinished += OnCinematicDialogueFinished;

        _cutsceneController.StartDialogue(
            _mate,
            new string[]
            {
                GameText.MateWakeUp,
                GameText.MateLessonEnding,
                GameText.MateBreakEnding,
                GameText.MateWakeUpInstruction,
                GameText.MateTakeBackpackInstruction
            },
            true
        );
    }

    private void OnCinematicDialogueFinished()
    {
        _mate.IntroFinished = true;
        _objectiveMarker.Show();
    }

    private void OnObjectPickedUp(ThrowableObject objectPickedUp)
    {
        if (objectPickedUp != _backpack)
            return;

        _objectiveMarker.Hide();
    }
}