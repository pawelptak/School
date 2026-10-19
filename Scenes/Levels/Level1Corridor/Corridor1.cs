using Godot;
public partial class Corridor1 : LevelBase
{
    private Mate _mate;
    private NpcController _studentController;
    private AsianStudentMale _student;
    private CutsceneController _cutsceneController;
    private ObjectiveMarker _objectiveMarker;

    private Player _player;
    private Camera3D _camera;

    public override void _Ready()
    {
        base._Ready();

        _mate = GetNode<Mate>("Mate");
        _mate.InteractionRequested += OnMateInteractionRequested;
        _cutsceneController = GetNode<CutsceneController>("CutsceneController");
        _objectiveMarker = GetNode<ObjectiveMarker>("ObjectiveMarker");
        _objectiveMarker.SetTarget(_mate);

        _student = GetNode<AsianStudentMale>("AsianStudentMale");
        _student.InteractionRequested += OnStudentInteractionRequested;

        _studentController = GetNode<NpcController>("AsianStudentMale/NpcController");
        _player = GetNode<Player>("Player");
        _camera = _player.GetNode<Camera3D>("Camera3D");

        _cutsceneController.SetCamera(_camera);
        _cutsceneController.SetPlayer(_player);

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
                new DialogueLine(DialogueSpeaker.Mate, Corridor1Text.HomeWorkQuestion, 3.0f),
                new DialogueLine(DialogueSpeaker.Player, Corridor1Text.HomeworkResponse, 2.5f),
                new DialogueLine(DialogueSpeaker.Player, Corridor1Text.HomeworkRequest, 2.5f),
                new DialogueLine(DialogueSpeaker.Mate, string.Empty, 2f),
                new DialogueLine(DialogueSpeaker.Mate, Corridor1Text.PlayerResponse2, 4.5f)
            ],
            onFinished: StartHomeworkMinigame,
            cinematic: true
        );
    }

    private void StartHomeworkMinigame()
    {
        GetTree().ChangeSceneToFile("res://Scenes/Levels/HomeworkMinigame/homework.tscn");
    }

    private void OnStudentInteractionRequested(Player player)
    {
        _cutsceneController.StartDialogue(
            _student,
            [
                new DialogueLine(DialogueSpeaker.AsianStudentMale, Corridor1Text.AsianStudentInteraction, 3.0f),
            ]
        );
    }
}
