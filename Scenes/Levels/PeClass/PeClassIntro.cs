using Godot;

public partial class PeClassIntro : LevelBase
{
    private Player _player;
    private Mate _mate;
    private Camera3D _playerCamera;
    private Camera3D _onCrowdCamera;
    private Camera3D _onTeacherCameraFront;
	private CutsceneController _cutsceneController;
	private PeTeacher _peTeacher;
    private NpcController _teacherController;
    private Marker3D _teacherGoToMarker;
    private Marker3D _teacherInitialPosition;
    private GroupOfStudentsFront _groupOfStudentsFront;
    private GroupOfStudentsRear _groupOfStudentsRear;
    private Basketball _basketball; // TODO: change to doctor ball xd
    private RigidBody3D _goat; // TODO: change to goat
    private Camera3D _goatCamera;
    private WallTimer _wallTimer; // TODO: change to goat
    private Camera3D _timerCamera;
    private StaticBody3D _playerConstraints;
   // private Camera3D _teacherCameraClose;

    public override void _Ready()
	{
		base._Ready();
        _player = GetNode<Player>("Player");
        _playerCamera = _player.GetNode<Camera3D>("Camera3D");
        _onCrowdCamera = GetNode<Camera3D>("OnCrowdCamera");
        _onTeacherCameraFront = GetNode<Camera3D>("OnTeacherCameraFront");
        _cutsceneController = GetNode<CutsceneController>("CutsceneController");
        _cutsceneController.SetCamera(_playerCamera);
        _cutsceneController.SetPlayer(_player);
        _peTeacher = GetNode<PeTeacher>("PeTeacher");
        _teacherController = _peTeacher.GetNode<NpcController>("NpcController");
        _groupOfStudentsFront = GetNode<GroupOfStudentsFront>("GroupOfStudentsFront");
        _groupOfStudentsRear = GetNode<GroupOfStudentsRear>("GroupOfStudentsRear");

        _teacherGoToMarker = GetNode<Marker3D>("TeacherGoToMarker");
        _teacherInitialPosition = GetNode<Marker3D>("TeacherInitialPosition");

        _basketball = GetNode<Basketball>("Basketball");
        _goat = GetNode<RigidBody3D>("Goat");
        _goatCamera = GetNode<Camera3D>("GoatCamera");

        _wallTimer = GetNode<WallTimer>("WallTimer");
        _timerCamera = GetNode<Camera3D>("TimerCamera");

        _mate = GetNode<Mate>("Mate");
        _playerConstraints = GetNode<StaticBody3D>("PlayerConstraints");

        TogglePlayerCollisions(false);

        // _teacherCameraClose = GetNode<Camera3D>("TeacherCameraClose");

        _cutsceneController.StartDialogue(
                _groupOfStudentsFront,
                [new DialogueLine(DialogueSpeaker.Player, string.Empty, 3.0f)],
                cinematic: true,
                onFinished: ShowTeacher,
                keepPlayerLockedOnFinish: true,
                smoothCameraTransition: false,
                referenceCamera: _onCrowdCamera
            );
    }


    private void TogglePlayerCollisions(bool enable)
    {
        foreach (Node child in _playerConstraints.GetChildren())
        {
            if (child is CollisionShape3D collisionShape)
            {
                collisionShape.Disabled = !enable;
            }
        }
    }

    private void ShowTeacher()
    {
        _cutsceneController.StartDialogue(
            _peTeacher,
            [
                new DialogueLine(DialogueSpeaker.PeTeacher, PeClassText.PeTeacherIntro, 2.0f),
                new DialogueLine(DialogueSpeaker.PeTeacher, PeClassText.PeTeacherIntro2, 1.5f),
                new DialogueLine(DialogueSpeaker.PeTeacher, PeClassText.PeTeacherComment, 2.0f),
                new DialogueLine(DialogueSpeaker.PeTeacher, PeClassText.PeTeacherComment2, 2.0f),
                new DialogueLine(DialogueSpeaker.PeTeacher, PeClassText.PeTeacherObjective1, 2.0f),
                new DialogueLine(DialogueSpeaker.PeTeacher, PeClassText.PeTeacherObjective2, 4.0f),
            ],
            cinematic: true,
            smoothCameraTransition: false,
            referenceCamera: _onTeacherCameraFront,
            keepPlayerLockedOnFinish: true,
            onFinished: TeacherGo
        );
    }

    private async void ShowTeacher2()
    {
        _teacherController.MoveTo(_teacherInitialPosition);

        _cutsceneController.StartDialogue( // sets camera on teacher
            _groupOfStudentsFront,
            [
                new DialogueLine(DialogueSpeaker.PeTeacher, string.Empty, 0.1f)
            ],
            cinematic: true,
            smoothCameraTransition: false,
            referenceCamera: _onTeacherCameraFront,
            keepPlayerLockedOnFinish: true
        );

        await ToSignal(GetTree().CreateTimer(2.0f), SceneTreeTimer.SignalName.Timeout);

        _cutsceneController.StartDialogue(
            _peTeacher,
            [
                new DialogueLine(DialogueSpeaker.PeTeacher, PeClassText.PeTeacherObjective3, 2.0f),
                new DialogueLine(DialogueSpeaker.PeTeacher, PeClassText.PeTeacherObjective4, 1.0f),
                new DialogueLine(DialogueSpeaker.PeTeacher, PeClassText.PeTeacherObjective5, 2.0f) // nie moglem znalezc pilki do kopania
            ],
            cinematic: true,
            smoothCameraTransition: false,
            referenceCamera: _onTeacherCameraFront,
            keepPlayerLockedOnFinish: true,
            onFinished: ShowBall
        );

    }

    private void ShowBall()
    {
        _basketball.Position = new Vector3(-0.137f, 1.236f, -2.881f);

        _cutsceneController.StartDialogue(
            _peTeacher,
            [
                new DialogueLine(DialogueSpeaker.PeTeacher, PeClassText.PeTeacherObjective6, 2.0f),
                new DialogueLine(DialogueSpeaker.PeTeacher, PeClassText.PeTeacherObjective7, 2.0f),
                new DialogueLine(DialogueSpeaker.PeTeacher, PeClassText.PeTeacherObjective8, 2.0f),
                new DialogueLine(DialogueSpeaker.PeTeacher, PeClassText.PeTeacherObjective9, 2.0f), // rzut pilka lekarska
            ],
            cinematic: true,
            smoothCameraTransition: false,
            referenceCamera: _onTeacherCameraFront,
            keepPlayerLockedOnFinish: true,
            onFinished: ShowGoat
        );
    }

    private async void TeacherGo()
    {
        await ToSignal(GetTree().CreateTimer(2.0f), SceneTreeTimer.SignalName.Timeout);

        _teacherController.MoveTo(_teacherGoToMarker);

        //await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);

        _cutsceneController.StartDialogue(
            _groupOfStudentsFront,
            [new DialogueLine(DialogueSpeaker.Player, string.Empty, 2.0f)],
            cinematic: true,
            onFinished: ShowTeacher2,
            keepPlayerLockedOnFinish: true,
            smoothCameraTransition: false,
            referenceCamera: _onCrowdCamera,
            delaySeconds: 1.0f
        );
    }

    private void ShowGoat()
    {
        _cutsceneController.StartDialogue(
            _peTeacher,
            [
                new DialogueLine(DialogueSpeaker.PeTeacher, PeClassText.PeTeacherObjective10, 2.0f),
            ],
            cinematic: true,
            referenceCamera: _goatCamera,
            keepPlayerLockedOnFinish: true,
            onFinished: ShowTimer
        );
        return;
    }

    private void ShowTimer()
    {
        _cutsceneController.StartDialogue(
            _peTeacher,
            [
                new DialogueLine(DialogueSpeaker.PeTeacher, PeClassText.PeTeacherObjective11, 2.0f),
            ],
            cinematic: true,
            referenceCamera: _timerCamera,
            keepPlayerLockedOnFinish: true,
            delaySeconds: 1.0f,
            onFinished: BackToTeacher
        );
    }

    private void BackToTeacher()
    {
        _cutsceneController.StartDialogue(
            _peTeacher,
            [
                new DialogueLine(DialogueSpeaker.PeTeacher, PeClassText.PeTeacherObjective12, 2.0f),
            ],
            cinematic: true,
            onFinished: PlayComeOnDialogue
        );
    }

    private void PlayComeOnDialogue()
    {
        _groupOfStudentsRear.Hide();
        _groupOfStudentsFront.Scale = new Vector3(2.0f, 2.0f, 2.0f);
        _mate.Scale = new Vector3(1.0f, 1.0f, 1.0f);

        TogglePlayerCollisions(true);

        _cutsceneController.StartDialogue(
            _peTeacher,
            [
                new DialogueLine(DialogueSpeaker.PeTeacher, PeClassText.PeTeacherObjective13, 2.0f),
            ],
            cinematic: false
        );
    }
}
