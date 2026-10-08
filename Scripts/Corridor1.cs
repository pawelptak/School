using Godot;

public partial class Corridor1 : Node3D
{
    private Mate _mate;
    private CutsceneController _cutsceneController;
    private ObjectiveMarker _objectiveMarker;

    public override void _Ready()
    {
        _mate = GetNode<Mate>("Mate");
        _cutsceneController = GetNode<CutsceneController>("CutsceneController");
        _objectiveMarker = GetNode<ObjectiveMarker>("ObjectiveMarker");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("interact"))
            return;

        if (_cutsceneController.IsDialogueVisible)
            return;

        if (!_mate.PlayerInRange)
            return;

        _objectiveMarker.Hide();
        StartMateDialogue();

        GetViewport().SetInputAsHandled();
    }

    private void StartMateDialogue()
    {
        _cutsceneController.StartDialogue(
            _mate,
            new DialogueLine[]
            {
            new DialogueLine(
                DialogueSpeaker.Mate,
                Corridor1Text.MateHomeWorkQuestion
            ),
            new DialogueLine(
                DialogueSpeaker.Mate,
                Corridor1Text.MateHomeWorkFollowup
            ),
            new DialogueLine(
                DialogueSpeaker.Player,
                Corridor1Text.HomeworkPlayerResponse
            ),
            new DialogueLine(
                DialogueSpeaker.Player,
                Corridor1Text.HomeworkPlayerRequest
            ),
            new DialogueLine(
                DialogueSpeaker.Mate,
                Corridor1Text.MatePlayerResponse1
            ),
            new DialogueLine(
                DialogueSpeaker.Mate,
                Corridor1Text.MatePlayerResponse2
            )
            },
            onFinished: StartHomeworkMinigame
        );
    }

    private void StartHomeworkMinigame()
    {
        GetTree().ChangeSceneToFile("res://Scenes/Levels/homework.tscn");
    }
}
