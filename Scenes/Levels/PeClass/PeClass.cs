using Godot;

public partial class PeClass : LevelBase
{
    private WallTimerLabel _wallTimerLabel1;
    private WallTimerLabel _wallTimerLabel2;
    private GoatMinigameController _goatMinigameController;
    private CutsceneController _cutsceneController;

    private Player _player;
    private Camera3D _camera;

    private PeTeacher _teacher;
    private CountdownScreen _countdownScreen;

    public override void _Ready()
    {
        base._Ready();

        _wallTimerLabel1 = GetNode<WallTimer>("WallTimer1").GetNode<WallTimerLabel>("WallTimerLabel");
        _wallTimerLabel2 = GetNode<WallTimer>("WallTimer2").GetNode<WallTimerLabel>("WallTimerLabel");

        _player = GetNode<Player>("Player");
        _camera = _player.GetNode<Camera3D>("Camera3D");
        _teacher = GetNode<PeTeacher>("PeTeacher");

        _goatMinigameController = GetNode<GoatMinigameController>("Minigame");
        _goatMinigameController.MinigameFinished += OnMinigameFinished;

        _wallTimerLabel1.StartTimer();
        _wallTimerLabel2.StartTimer();

        _cutsceneController = GetNode<CutsceneController>("CutsceneController");
        _cutsceneController.SetPlayer(_player);
        _cutsceneController.SetCamera(_camera);

        GameManager.Instance.CurrentSaveData.AttackingUnlocked = false;
        _countdownScreen = GetNode<CountdownScreen>("CountdownScreen");
        _countdownScreen.StartCountdown();

        _countdownScreen.CountdownFinished += () => _goatMinigameController.StartMinigame();
    }

    private void OnMinigameFinished(int hits, int grade)
    {
        GameManager.Instance.CurrentSaveData.AttackingUnlocked = true;

        _cutsceneController.StartDialogue(
            _teacher,
            [
                new DialogueLine(DialogueSpeaker.PeTeacher, GetTeacherInteractionText(grade), 3.0f),
            ],
            onFinished: () => SceneTransition.Instance.ChangeScene("res://Scenes/Levels/PeClass/blocking_tutorial.tscn"),
            cinematic: true,
            isSkippable: false
        );
    }

    private string GetTeacherInteractionText(int homeworkGrade)
    {
        return homeworkGrade switch
        {
            1 => PeClassText.Grade1Text,
            2 => PeClassText.Grade2Text,
            3 => PeClassText.Grade3Text,
            4 => PeClassText.Grade4Text,
            5 => PeClassText.Grade5Text,
            6 => PeClassText.Grade6Text,
            _ => string.Empty
        };
    }


    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
