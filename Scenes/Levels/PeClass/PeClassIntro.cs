using Godot;

public partial class PeClassIntro : LevelBase
{
    private Player _player;
    private Camera3D _playerCamera;
    private Camera3D _onCrowdCamera;
    private Camera3D _onTeacherCameraFront;
	private CutsceneController _cutsceneController;
	private PeTeacher _peTeacher;

    public override void _Ready()
	{
		base._Ready();
        _player = GetNode<Player>("Player");
        _playerCamera = _player.GetNode<Camera3D>("Camera3D");
        _onCrowdCamera = GetNode<Camera3D>("OnCrowdCamera");
        _onTeacherCameraFront = GetNode<Camera3D>("OnTeacherCameraFront");
        _cutsceneController = GetNode<CutsceneController>("CutsceneController");
        _cutsceneController.SetCamera(_onCrowdCamera);
        _cutsceneController.SetPlayer(_player);
        _peTeacher = GetNode<PeTeacher>("PeTeacher");

        //_cutsceneController.StartDialogue(
        //    _peTeacher,
        //    [
        //        new DialogueLine(DialogueSpeaker.PeTeacher, PolishClassText.Introduction, 2.0f),
        //    ],
        //    cinematic: true,
        //    onFinished: SwitchToMate,
        //    keepPlayerLockedOnFinish: true,
        //    delaySeconds: 2.0f
        //);

        _cutsceneController.StartDialogue(
            _peTeacher,
            [
                new DialogueLine(DialogueSpeaker.PeTeacher, PolishClassText.Introduction, 2.0f),
            ],
            cinematic: true,
            onFinished: SwitchToMate,
            keepPlayerLockedOnFinish: true,
            delaySeconds: 2.0f
        );

    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
