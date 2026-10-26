using Godot;

public partial class BlockingTutorial : Node3D
{
    private CutsceneController _cutsceneController;

    private Player _player;
    private Camera3D _camera;

    private PeTeacher _teacher;
    private NpcController _teacherController;

    private Door _doorLeft;
    private Door _doorRight;

    private int _blockCount = 0;

    public override void _Ready()
    {
        _cutsceneController = GetNode<CutsceneController>("CutsceneController");
        _player = GetNode<Player>("Player");
        _camera = _player.GetNode<Camera3D>("Camera3D");
        _teacher = GetNode<PeTeacher>("PeTeacher");
        _teacherController = _teacher.GetNode<NpcController>("NpcController");
        _cutsceneController.SetPlayer(_player);
        _cutsceneController.SetCamera(_camera);
        _doorLeft = GetNode<Door>("GymnasiumDoorLeft");
        _doorRight = GetNode<Door>("GymnasiumDoorRight");

        _doorLeft.Interacted += OnGymnasiumDoorInteracted;
        _doorRight.Interacted += OnGymnasiumDoorInteracted;

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

        _player.AttackBlocked += OnAttackBlocked;
    }

    private void StartTutorial()
    {
        GameManager.Instance.CurrentSaveData.BlockingUnlocked = true;
        _teacherController.StartChase(_player);
    }

    private void OnAttackBlocked(Node3D attacker)
    {
        if (attacker != _teacher)
            return;

        _blockCount++;

        GD.Print($"Blocking tutorial: {_blockCount}/3");

        if (_blockCount >= 3)
        {
            GD.Print("Blocking tutorial completed!");

            _teacherController.SetIdleState();

            _cutsceneController.StartDialogue(
                _teacher,
                [
                    new DialogueLine(DialogueSpeaker.PeTeacher, PeClassText.BlockingTutorialCompleted1, 3.0f),
                    new DialogueLine(DialogueSpeaker.PeTeacher, PeClassText.BlockingTutorialCompleted2, 2.0f)
                ],
                cinematic: true,
                delaySeconds: 1.0f,
                onFinished: () =>
                {
                    _doorLeft.InteractionEnabled = true;
                    _doorRight.InteractionEnabled = true;
                }
            );

            _teacher.InteractionRequested += (_player) =>
            {
                _cutsceneController.StartDialogue(
                    _teacher,
                    [new DialogueLine(DialogueSpeaker.PeTeacher, PeClassText.BlockingTutorialCompleted2, 2.0f)]
                );
            };
        }
    }

    public override void _ExitTree()
    {
        if (_player != null && GodotObject.IsInstanceValid(_player))
            _player.AttackBlocked -= OnAttackBlocked;
    }

    private void OnGymnasiumDoorInteracted()
    {
        SceneTransition.Instance.ChangeScene("res://Scenes/Levels/DownstairsFight/downstairs_fight.tscn");
    }
}
