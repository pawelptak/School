using Godot;

public partial class BlockingTutorial : Node3D
{
    private CutsceneController _cutsceneController;

    private Player _player;
    private Camera3D _camera;

    private PeTeacher _teacher;
    private NpcController _teacherController;

    public override void _Ready()
    {
        _cutsceneController = GetNode<CutsceneController>("CutsceneController");
        _player = GetNode<Player>("Player");
        _camera = _player.GetNode<Camera3D>("Camera3D");
        _teacher = GetNode<PeTeacher>("PeTeacher");
        _teacherController = _teacher.GetNode<NpcController>("NpcController");
        _cutsceneController.SetPlayer(_player);
        _cutsceneController.SetCamera(_camera);

        _cutsceneController.StartDialogue(
            _teacher,
            [
                new DialogueLine(DialogueSpeaker.PeTeacher, PeClassText.BlockingTutorial1, 3.0f),
                new DialogueLine(DialogueSpeaker.PeTeacher, PeClassText.BlockingTutorial2, 3.0f),
                new DialogueLine(DialogueSpeaker.PeTeacher, PeClassText.BlockingTutorial3, 3.0f),
                new DialogueLine(DialogueSpeaker.PeTeacher, string.Format(PeClassText.BlockingTutorial4, "PPM"), 3.0f),
                new DialogueLine(DialogueSpeaker.PeTeacher, PeClassText.BlockingTutorial5, 3.0f)
            ],
            cinematic: true,
            delaySeconds: 2.0f,
            onFinished: StartTutorial
        );
    }

    private void StartTutorial()
    {
        GameManager.Instance.CurrentSaveData.BlockingUnlocked = true;
        _teacherController.StartChase(_player);
    }
}
