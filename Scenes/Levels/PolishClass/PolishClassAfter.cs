using Godot;
using School.Scenes.Levels.PolishClass;

public partial class PolishClassAfter : LevelBase
{
    private Mate _mate;
    private Player _player;
    private Camera3D _camera;
    private PolishTeacher _teacher;
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
            [new DialogueLine(DialogueSpeaker.Mate, PolishClassText.AfterClass, 3.0f)],
            cinematic: true,
            onFinished: ShowGradePopup,
            delaySeconds: 1.0f
        );

        _homeWorkMinigameGrade = GameManager.Instance.GetMinigameResult("homework_minigame").Grade;

        _teacher = GetNode<PolishTeacher>("PolishTeacher");
        _teacher.InteractionRequested += (_player) =>
        {
            var teacherInteractionText = GetTeacherInteractionText(_homeWorkMinigameGrade);
            _cutsceneController.StartDialogue(
                _teacher,
                [new DialogueLine(DialogueSpeaker.PolishTeacher, teacherInteractionText, 3.0f)]
            );
        };
    }

    private string GetTeacherInteractionText(int homeworkGrade)
    {
        return homeworkGrade switch
        {
            1 => PolishClassText.Grade1Interaction,
            2 => PolishClassText.Grade2Interaction,
            3 => PolishClassText.Grade3Interaction,
            4 => PolishClassText.Grade4Interaction,
            5 => PolishClassText.Grade5Interaction,
            6 => PolishClassText.Grade6Interaction,
            _ => string.Empty
        };
    }

    private string GetMateReactionText(int homeworkGrade)
    {
        return homeworkGrade switch
        {
            1 => PolishClassText.Grade1Response,
            2 => PolishClassText.Grade2Response,
            3 => PolishClassText.Grade3Response,
            4 => PolishClassText.Grade4Response,
            5 => PolishClassText.Grade5Response,
            6 => PolishClassText.Grade6Response,
            _ => string.Empty
        };
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
            PolishClassText.JournalUnlockedPopupTitle,
            string.Format(PolishClassText.JournalUnlockedPopupText, GameManager.Instance.GetActionKeyName("toggle_grade_journal"))
        );

        GameManager.Instance.CurrentSaveData.GradeJournalUnlocked = true;

        _mate.InteractionRequested += (_player) =>
        {
            _cutsceneController.StartDialogue(
                _mate,
                [new DialogueLine(DialogueSpeaker.Mate, string.Format(PolishClassText.OpenJournalInstruction, GameManager.Instance.GetActionKeyName("toggle_grade_journal")), 3.0f)]
            );
        };
    }

    private void OnGradeJournalClosed()
    {
        if (GradeJournalClosed)
            return;

        GradeJournalClosed = true;

        var nextDialogueText = GetMateReactionText(_homeWorkMinigameGrade);

        _cutsceneController.StartDialogue(
            _mate,
            [
                new DialogueLine(DialogueSpeaker.Mate, nextDialogueText, 3.0f),
                new DialogueLine(DialogueSpeaker.Mate, PolishClassText.ExitPolishClass, 2.0f)],
            onFinished: () =>
            {
                _door.InteractionEnabled = true;
            }
        );

        _mate.InteractionRequested += (_player) =>
        {
            _cutsceneController.StartDialogue(
                _mate,
                [new DialogueLine(DialogueSpeaker.Mate, PolishClassText.ExitPolishClass, 2.0f)]
            );
        };
    }
}
