using Godot;

public partial class Corridor1 : Node3D
{
    private Mate _mate;
    private CutsceneController _cutsceneController;
    private ObjectiveMarker _objectiveMarker;

    public override void _Ready()
    {
        _mate = GetNode<Mate>("Mate");
        _mate.InteractionRequested += OnMateInteractionRequested;
        _cutsceneController = GetNode<CutsceneController>("CutsceneController");
        _objectiveMarker = GetNode<ObjectiveMarker>("ObjectiveMarker");
        _objectiveMarker.SetTarget(_mate);
    }

    private void OnMateInteractionRequested(Player player)
    {
        _objectiveMarker.Hide();
        StartMateDialogue();
    }

    private void StartMateDialogue()
    {
        _cutsceneController.StartDialogue(
            _mate,
            [
                new DialogueLine(DialogueSpeaker.Mate, MateText.HomeWorkQuestion, 3.0f),
                new DialogueLine(DialogueSpeaker.Player, PlayerText.HomeworkResponse, 2.5f),
                new DialogueLine(DialogueSpeaker.Player, PlayerText.HomeworkRequest, 2.5f),
                new DialogueLine(DialogueSpeaker.Mate, string.Empty, 2f),
                new DialogueLine(DialogueSpeaker.Mate, MateText.PlayerResponse2, 4.5f)
            ],
            onFinished: StartHomeworkMinigame
        );
    }

    private void StartHomeworkMinigame()
    {
        GetTree().ChangeSceneToFile("res://Scenes/Levels/HomeworkMinigame/homework.tscn");
    }
}
