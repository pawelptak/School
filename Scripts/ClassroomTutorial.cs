using Godot;

public partial class ClassroomTutorial : Node3D
{
    private Mate _mate;
    private Player _player;
    private CutsceneController _cutsceneController;
    private Node3D _objectiveMarker;
    private ThrowableObject _backpack;
    private Node3D _door;

    private bool _backpackObjectiveActive;
    private bool _doorObjectiveActive;

    public override void _Ready()
    {
        _mate = GetNode<Mate>("Mate");
        _player = GetNode<Player>("Player");
        _cutsceneController = GetNode<CutsceneController>("CutsceneController");
        _objectiveMarker = GetNode<Node3D>("ObjectiveMarker");
        _backpack = GetNode<ThrowableObject>("Backpack");
        _door = GetNode<Node3D>("Door");

        _objectiveMarker.Hide();

        _backpackObjectiveActive = false;
        _doorObjectiveActive = false;

        _player.ObjectPickedUp += OnObjectPickedUp;
        _mate.GoToDoor += OnGoToDoor;

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

        GD.Print("TUTORIAL ZAKOŃCZONY");
    }

    private void OnCinematicDialogueFinished()
    {
        _mate.IntroFinished = true;

        _backpackObjectiveActive = true;
        _objectiveMarker.Show();
    }

    private void OnObjectPickedUp(ThrowableObject objectPickedUp)
    {
        if (objectPickedUp != _backpack)
            return;

        _backpackObjectiveActive = false;
        _objectiveMarker.Hide();
    }

    private void OnGoToDoor()
    {
        _doorObjectiveActive = true;

        _objectiveMarker.GlobalPosition =
            _door.GlobalPosition + new Vector3(0, 2f, 0);

        _objectiveMarker.Show();
    }
}