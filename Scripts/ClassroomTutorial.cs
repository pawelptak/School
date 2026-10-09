using Godot;

public partial class ClassroomTutorial : Node3D
{
    private Mate _mate;
    private Player _player;
    private CutsceneController _cutsceneController;
    private ObjectiveMarker _objectiveMarker;
    private Backpack _backpack;
    private bool _doorObjectiveActive;
    private Door _door;

    public override void _Ready()
    {
        _mate = GetNode<Mate>("Mate");
        _player = GetNode<Player>("Player");
        _cutsceneController = GetNode<CutsceneController>("CutsceneController");
        _cutsceneController.SetPlayer(_player);
        _cutsceneController.SetCamera(_player.GetNode<Camera3D>("Camera3D"));

        _objectiveMarker = GetNode<ObjectiveMarker>("ObjectiveMarker");
        _backpack = GetNode<Backpack>("Backpack");
        _door = GetNode<Door>("Door");
        _door.InteractionEnabled = false;
        _door.Interacted += OnDoorInteracted;

        _mate.HitReactionOverride = TryHandleMateHit;

        _objectiveMarker.Hide();

        _player.ObjectPickedUp += OnObjectPickedUp;

        _cutsceneController.CinematicDialogueFinished += OnCinematicDialogueFinished;

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
                    string.Format(TutorialText.MateTakeBackpackInstruction, "LPM")
                )
            },
            true
        );
    }

    private void OnDoorInteracted()
    {
        GetTree().ChangeSceneToFile("res://Scenes/Levels/corridor_1.tscn");
    }

    private void OnCinematicDialogueFinished()
    {
        _objectiveMarker.SetTarget(_backpack);
        _objectiveMarker.Show();
    }

    private void OnObjectPickedUp(ThrowableObject objectPickedUp)
    {
        if (objectPickedUp != _backpack || _doorObjectiveActive)
            return;

        _objectiveMarker.SetTarget(_mate);
    }

    private bool TryHandleMateHit(ThrowableObject throwable, float throwForce)
    {
        if (throwable != _backpack)
        {
            _cutsceneController.StartDialogue(_mate, new DialogueLine[]
            {
                new DialogueLine(
                    DialogueSpeaker.Mate,
                    TutorialText.MateThrowWrongItem
                )
            });

            return true;
        }

        if (_doorObjectiveActive)
            return false;

        if (throwForce < 15.0f)
        {
            _cutsceneController.StartDialogue(_mate, new DialogueLine[]
            {
                new DialogueLine(
                    DialogueSpeaker.Mate,
                    string.Format(TutorialText.MateThrowAgain, "LPM")
                )
            });

            return true;
        }

        _objectiveMarker.Hide();

        _cutsceneController.StartDialogue(
            _mate,
            new DialogueLine[]
            {
                new DialogueLine(
                    DialogueSpeaker.Mate,
                    TutorialText.MateThrowGood
                ),
                new DialogueLine(
                    DialogueSpeaker.Mate,
                    TutorialText.MateGoToDoor
                )
            },
            onFinished: OnGoToDoor
        );

        return true;
    }

    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_mate))
        {
            _mate.HitReactionOverride = null;
        }
    }

    private void OnGoToDoor()
    {
        _door.InteractionEnabled = true;

        _doorObjectiveActive = true;

        _objectiveMarker.TargetOffset = new Vector3(0, 1, -0.6f);
        _objectiveMarker.SetTarget(_door);

        _objectiveMarker.Show();
    }
}
