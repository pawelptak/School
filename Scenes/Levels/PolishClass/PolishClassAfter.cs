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
            [new DialogueLine(DialogueSpeaker.Mate, MateText.AfterClass, 3.0f)],
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
            1 => PolishTeacherText.Grade1Interaction,
            2 => PolishTeacherText.Grade2Interaction,
            3 => PolishTeacherText.Grade3Interaction,
            4 => PolishTeacherText.Grade4Interaction,
            5 => PolishTeacherText.Grade5Interaction,
            6 => PolishTeacherText.Grade6Interaction,
            _ => string.Empty
        };
    }

    private string GetMateReactionText(int homeworkGrade)
    {
        return homeworkGrade switch
        {
            1 => MateText.Grade1Response,
            2 => MateText.Grade2Response,
            3 => MateText.Grade3Response,
            4 => MateText.Grade4Response,
            5 => MateText.Grade5Response,
            6 => MateText.Grade6Response,
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
            PolishClassAfterText.JournalUnlockedPopupTitle,
            string.Format(PolishClassAfterText.JournalUnlockedPopupText, GameManager.Instance.GetActionKeyName("toggle_grade_journal"))
        );

        GameManager.Instance.CurrentSaveData.GradeJournalUnlocked = true;

        _mate.InteractionRequested += (_player) =>
        {
            _cutsceneController.StartDialogue(
                _mate,
                [new DialogueLine(DialogueSpeaker.Mate, string.Format(MateText.OpenJournalInstruction, GameManager.Instance.GetActionKeyName("toggle_grade_journal")), 3.0f)]
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
                new DialogueLine(DialogueSpeaker.Mate, MateText.ExitPolishClass, 2.0f)],
            onFinished: () =>
            {
                _door.InteractionEnabled = true;
            }
        );

        _mate.InteractionRequested += (_player) =>
        {
            _cutsceneController.StartDialogue(
                _mate,
                [new DialogueLine(DialogueSpeaker.Mate, MateText.ExitPolishClass, 2.0f)]
            );
        };
    }
}
