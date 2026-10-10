using Godot;

public partial class PolishClass : Node3D
{
    private Mate _mate;
    private PolishTeacher _teacher;
    private Player _player;
    private CutsceneController _cutsceneController;

    public override void _Ready()
    {
        _mate = GetNode<Mate>("Mate");
        _teacher = GetNode<PolishTeacher>("PolishTeacher");
        _player = GetNode<Player>("Player");
        _cutsceneController = GetNode<CutsceneController>("CutsceneController");
        _cutsceneController.SetPlayer(_player);
        _cutsceneController.SetCamera(_player.GetNode<Camera3D>("Camera3D"));

        _cutsceneController.StartDialogue(
            _teacher,
            [
                new DialogueLine(DialogueSpeaker.PolishTeacher, PolishTeacherText.Introduction, 3.5f),
                new DialogueLine(DialogueSpeaker.PolishTeacher, PolishTeacherText.LessonTopic, 3.5f),
                new DialogueLine(DialogueSpeaker.PolishTeacher, PolishTeacherText.LessonTopic, 3.5f)
            ],
            cinematic: true,
            onFinished: SwitchToMate,
            keepPlayerLockedOnFinish: true,
            delaySeconds: 2.0f
        );
    }

    private void SwitchToMate()
    {
        var customCameraPosition = GetNode<Node3D>("CustomCameraFar").GlobalPosition;

        _cutsceneController.StartDialogue(
            _mate,
            [new DialogueLine(DialogueSpeaker.Mate, MateText.PhysicalEdu, 3.5f)],
            cinematic: true,
            onFinished: BackToTeacher,
            keepPlayerLockedOnFinish: true,
            customCameraPosition
        );
    }

    private void BackToTeacher()
    {
        _cutsceneController.StartDialogue(
            _teacher,
            [new DialogueLine(DialogueSpeaker.PolishTeacher, PolishTeacherText.Homework, 3.5f)],
            cinematic: true,
            onFinished: BackToMate,
            keepPlayerLockedOnFinish: true
        );
    }

    private void BackToMate()
    {
        var customCameraPosition = GetNode<Node3D>("CustomCameraClose").GlobalPosition;

        _cutsceneController.StartDialogue(
            _mate,
            [new DialogueLine(DialogueSpeaker.Mate, string.Empty, 3.5f)],
            cinematic: true,
            onFinished: null,
            keepPlayerLockedOnFinish: true,
            customCameraPosition
        );
    }
}
