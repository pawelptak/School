using Godot;
using School.Scenes.Levels.PolishClass;

public partial class PolishClassAfter : LevelBase
{
    private Mate _mate;
    private Player _player;
    private Camera3D _camera;
    private Door _door;
    private CutsceneController _cutsceneController;
    private GradeJournal _gradeJournal;
    private bool GradeJournalClosed = false;

    private int _homeWorkMinigameGrade;

    public override void _Ready()
    {
        base._Ready();

        _door = GetNode<Door>("Door");
        _mate = GetNode<Mate>("Mate");
        _player = GetNode<Player>("Player");
        _camera = _player.GetNode<Camera3D>("Camera3D");
        _cutsceneController = GetNode<CutsceneController>("CutsceneController");
        _cutsceneController.SetPlayer(_player);
        _cutsceneController.SetCamera(_camera);

        _gradeJournal = GetNode<GradeJournal>("/root/GradeJournal");
        _gradeJournal.JournalClosed += OnGradeJournalClosed;
        _door.Interacted += OnDoorInteracted;

        _cutsceneController.StartDialogue(
            _mate,
            [new DialogueLine(DialogueSpeaker.Mate, MateText.AfterClass, 3.0f)],
            cinematic: true,
            onFinished: ShowGradePopup,
            delaySeconds: 1.0f
        );

        _homeWorkMinigameGrade = GameManager.Instance.GetMinigameResult("homework_minigame").Grade;
        // TODO: add teacher interaction text based on grade
        // TODO: add on mate intraction open Journal instruction
    }

    private void OnDoorInteracted()
    {
        SceneTransition.Instance.ChangeScene("res://Scenes/Levels/CorridorFight1/corridor_fight_1.tscn");
    }

    private async void ShowGradePopup()
    {
        var popup = GetNode<Popup>("Popup");

        // Wait for 1 second before showing the grade popup
        await ToSignal(GetTree().CreateTimer(1.0f), "timeout");

        popup.ShowPopup(
            PolishClassAfterText.JournalUnlockedPopupTitle,
            string.Format(PolishClassAfterText.JournalUnlockedPopupText, GameManager.Instance.GetActionKeyName("toggle_grade_journal"))
        );

        GameManager.Instance.CurrentSaveData.GradeJournalUnlocked = true;

    }

    private void OnGradeJournalClosed()
    {
        if (GradeJournalClosed)
            return;

        GradeJournalClosed = true;

        string nextDialogueText = string.Empty;

        switch (_homeWorkMinigameGrade)
        {
            case 1:
                nextDialogueText = MateText.Grade1Response;
                break;
            case 2:
                nextDialogueText = MateText.Grade2Response;
                break;
            case 3:
                nextDialogueText = MateText.Grade3Response;
                break;
            case 4:
                nextDialogueText = MateText.Grade4Response;
                break;
            case 5:
                nextDialogueText = MateText.Grade5Response;
                break;
            case 6:
                nextDialogueText = MateText.Grade6Response;
                break;
            default:
                break;
        }
        
        _cutsceneController.StartDialogue(
            _mate,
            [
                new DialogueLine(DialogueSpeaker.Mate, nextDialogueText, 3.0f),
                new DialogueLine(DialogueSpeaker.Mate, MateText.ExitPolishClass, 2.0f)],
            onFinished: () =>
            {
                _door.InteractionEnabled = true;
            }
        );
    }
}
