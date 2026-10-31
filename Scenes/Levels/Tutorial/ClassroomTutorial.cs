using Godot;
using System;
public partial class ClassroomTutorial : LevelBase
{
    private Mate _mate;
    private Player _player;
    private Camera3D _camera;
    private CutsceneController _cutsceneController;
    private ObjectiveMarker _objectiveMarker;
    private Backpack _backpack;
    private bool _doorObjectiveActive;
    private Door _door;
    private RigidBody3D _playerChair;
    private RigidBody3D _playerDesk;
    private int correctThrowCount = 0;

    private ReadableNote _announcement;

    public override void _Ready()
    {
        base._Ready();

        _mate = GetNode<Mate>("Mate");
        _player = GetNode<Player>("Player");
        _camera = _player.GetNode<Camera3D>("Camera3D");
        _playerChair = GetNode<RigidBody3D>("PlayerChair");
        _playerDesk = GetNode<RigidBody3D>("PlayerDesk");

        _cutsceneController = GetNode<CutsceneController>("CutsceneController");
        _cutsceneController.SetPlayer(_player);
        _cutsceneController.SetCamera(_camera);

        _objectiveMarker = GetNode<ObjectiveMarker>("ObjectiveMarker");
        _backpack = GetNode<Backpack>("Backpack");
        _door = GetNode<Door>("Door");
        _door.InteractionEnabled = false;
        _door.Interacted += OnDoorInteracted;

        _mate.HitReactionOverride = TryHandleMateHit;

        _objectiveMarker.Hide();

        _player.ObjectPickedUp += OnObjectPickedUp;

        _playerChair.Freeze = true;
        _playerDesk.Freeze = true;
        _player.MovementLocked = true;
        _player.CameraLocked = true;

        SetupInitialCamera();

        PlayCameraIntroSequence();

        _announcement = GetNode<ReadableNote>("WallAnnouncement");
        _announcement.NoteContent = string.Format(_announcement.NoteContent, DateTime.Now.AddDays(1).ToString("dd.MM.yyyy"));
    }

    private void SetupInitialCamera()
    {
        _camera.Position = new Vector3(0.0f, 0.205f, -1.07f);
        _camera.Fov = 50.0f;
        _camera.RotationDegrees = new Vector3(-42.0f, 0.0f, 0.0f);
    }

    private void PlayCameraIntroSequence()
    {
        var tween = CreateTween();
        tween.SetProcessMode(Tween.TweenProcessMode.Idle);

        // Waking up
        tween.TweenProperty(_camera, "rotation_degrees", new Vector3(1.0f, 0.0f, 0.0f), 1.2f)
            .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);

        // Looking left
        tween.TweenProperty(_camera, "rotation_degrees", new Vector3(-0.6f, 34.2f, 2.3f), 1.5f)
            .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);

        tween.TweenInterval(0.8f);

        // Looking right
        tween.TweenProperty(_camera, "rotation_degrees", new Vector3(-5.6f, -46.2f, -2.6f), 1.5f)
            .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);

        tween.TweenInterval(0.3f);

        // Looking up to Mate
        tween.TweenProperty(_camera, "rotation_degrees", new Vector3(7.9f, -45.9f, 10.4f), 0.4f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);

        // Standing up from the desk
        tween.Parallel();
        tween.TweenProperty(_camera, "position", new Vector3(0.0f, 0.7f, 0.0f), 0.6f);
        tween.TweenProperty(_camera, "rotation_degrees", Vector3.Zero, 0.6f);
        tween.TweenProperty(_camera, "fov", 75.0f, 0.6f);

        tween.Finished += OnIntroFinished;
    }

    private void OnIntroFinished()
    {
        _playerChair.Freeze = false;
        _playerDesk.Freeze = false;
        _player.MovementLocked = false;
        _player.CameraLocked = false;
        StartDialogueAfterIntro();
    }

    private void StartDialogueAfterIntro()
    {
        _cutsceneController.StartDialogue(
            _mate,
            [
                new DialogueLine(DialogueSpeaker.Mate, TutorialText.WakeUp, 3.5f),
                new DialogueLine(DialogueSpeaker.Mate, TutorialText.LessonEnding, 4.5f),
                new DialogueLine(DialogueSpeaker.Mate, TutorialText.BreakEnding, 3.5f),
                new DialogueLine(DialogueSpeaker.Mate, TutorialText.WakeUpInstruction, 2.5f),
                new DialogueLine(DialogueSpeaker.Mate, string.Format(TutorialText.TakeBackpackInstruction, "LPM"), 6.0f),
            ],
            cinematic: true,
            onFinished: OnCinematicDialogueFinished,
            delaySeconds: 0.2f
        );
    }

    private void OnDoorInteracted()
    {
        SceneTransition.Instance.ChangeScene("res://Scenes/Levels/Level1Corridor/corridor_1_inherited.tscn");
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
            _cutsceneController.StartDialogue(_mate, [new DialogueLine(DialogueSpeaker.Mate, TutorialText.ThrowWrongItem, 2.0f)]);

            return true;
        }

        if (_doorObjectiveActive)
            return false;


        if (correctThrowCount < 1)
        {
            _cutsceneController.StartDialogue(_mate, [new DialogueLine(DialogueSpeaker.Mate, string.Format(TutorialText.ThrowAgain, "LPM"), 4.0f)]);

            correctThrowCount++;

            return true;
        }



        _objectiveMarker.Hide();

        OnGoToDoor();

        _cutsceneController.StartDialogue(
            _mate,
            [
                new DialogueLine(DialogueSpeaker.Mate, TutorialText.ThrowGood, 2.5f),
                new DialogueLine(DialogueSpeaker.Mate, TutorialText.GoToDoor, 4.5f)
            ]
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
