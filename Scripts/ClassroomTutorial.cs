using Godot;

public partial class ClassroomTutorial : Node3D
{
    private Mate _mate;
    private Player _player;
    private CutsceneController _cutsceneController;
    private Node3D _objectiveMarker;
    private Backpack _backpack;
    private bool _doorObjectiveActive;
    private Node3D _door;

    public override void _Ready()
    {
        _mate = GetNode<Mate>("Mate");
        _player = GetNode<Player>("Player");
        _cutsceneController = GetNode<CutsceneController>("CutsceneController");
        _objectiveMarker = GetNode<Node3D>("ObjectiveMarker");
        _backpack = GetNode<Backpack>("Backpack");
        _door = GetNode<Node3D>("Door");

        _mate.GoToDoor += OnGoToDoor;

        _objectiveMarker.Hide();

        _player.ObjectPickedUp += OnObjectPickedUp;

        _cutsceneController.CinematicDialogueFinished += OnCinematicDialogueFinished;

        DisableBackpackCollisions();

        _cutsceneController.StartDialogue(
            _mate,
            new DialogueLine[]
            {
                new DialogueLine(
                    DialogueSpeaker.Mate,
                    TutorialText.MateWakeUp
                ),
                new DialogueLine(
                    DialogueSpeaker.Mate,
                    TutorialText.MateLessonEnding
                ),
                new DialogueLine(
                    DialogueSpeaker.Mate,
                    TutorialText.MateBreakEnding
                ),
                new DialogueLine(
                    DialogueSpeaker.Mate,
                    TutorialText.MateWakeUpInstruction
                ),
                new DialogueLine(
                    DialogueSpeaker.Mate,
                    TutorialText.MateTakeBackpackInstruction
                )
            },
            true
        );
    }

    public override void _Process(double delta)
    {
        if (!_doorObjectiveActive)
            return;

        if (!Input.IsActionJustPressed("interact"))
            return;

        var ray = _player.GetNode<RayCast3D>("Camera3D/InteractRay");

        if (!ray.IsColliding())
            return;

        if (ray.GetCollider() != _door)
            return;

        GetTree().ChangeSceneToFile("res://Scenes/corridor_1.tscn");
    }

    private void OnCinematicDialogueFinished()
    {
        _objectiveMarker.Show();
    }

    private void OnObjectPickedUp(ThrowableObject objectPickedUp)
    {
        if (objectPickedUp != _backpack)
            return;

        EnableBackpackCollisions();

        _objectiveMarker.Hide();
    }

    private void OnGoToDoor()
    {
        _doorObjectiveActive = true;

        _objectiveMarker.GlobalPosition =
            _door.GlobalPosition + new Vector3(0, 2f, 0);

        _objectiveMarker.Show();
    }

    private void EnableBackpackCollisions()
    {
        _backpack.RemoveCollisionExceptionWith(_player);

        foreach (var node in GetTree().GetNodesInGroup("chairs"))
        {
            if (node is PhysicsBody3D chair)
            {
                _backpack.RemoveCollisionExceptionWith(chair);
            }
        }
    }

    private void DisableBackpackCollisions()
    {
        _backpack.AddCollisionExceptionWith(_player);

        foreach (var node in GetTree().GetNodesInGroup("chairs"))
        {
            if (node is PhysicsBody3D chair)
            {
                _backpack.AddCollisionExceptionWith(chair);
            }
        }
    }
}
