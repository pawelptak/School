using Godot;
public partial class PolishClass : Node3D
{
    private Mate _mate;
    private PolishTeacher _teacher;
    private PolishTeacher _teacher2;
    private Player _player;
    private CutsceneController _cutsceneController;
    private Clock _clock;

    public override void _Ready()
    {
        _mate = GetNode<Mate>("Mate");
        _teacher = GetNode<PolishTeacher>("PolishTeacher");
        _teacher2 = GetNode<PolishTeacher>("PolishTeacher2");
        _clock = GetNode<Clock>("Clock");
        _player = GetNode<Player>("Player");
        _cutsceneController = GetNode<CutsceneController>("CutsceneController");
        _cutsceneController.SetPlayer(_player);
        _cutsceneController.SetCamera(_player.GetNode<Camera3D>("Camera3D"));

        _cutsceneController.StartDialogue(
            _teacher,
            [
                new DialogueLine(DialogueSpeaker.PolishTeacher, PolishClassText.Introduction, 3.5f),
                new DialogueLine(DialogueSpeaker.PolishTeacher, PolishClassText.LessonTopic, 3.5f),
                new DialogueLine(DialogueSpeaker.PolishTeacher, PolishClassText.LessonTopic, 3.5f)
            ],
            cinematic: true,
            onFinished: SwitchToMate,
            keepPlayerLockedOnFinish: true,
            delaySeconds: 2.0f
        );
    }

    private void SwitchToMate()
    {
        var customCamera = GetNode<Camera3D>("CustomCameraFar");

        _cutsceneController.StartDialogue(
            _mate,
            [new DialogueLine(DialogueSpeaker.Mate, PolishClassText.PhysicalEdu, 3.5f)],
            cinematic: true,
            onFinished: BackToTeacher,
            keepPlayerLockedOnFinish: true,
            referenceCamera: customCamera
        );
    }

    private void BackToTeacher()
    {
        _cutsceneController.StartDialogue(
            _teacher,
            [new DialogueLine(DialogueSpeaker.PolishTeacher, PolishClassText.Homework, 3.5f)],
            cinematic: true,
            onFinished: BackToMate,
            keepPlayerLockedOnFinish: true
        );
    }

    private void BackToMate()
    {
        var customCamera = GetNode<Camera3D>("CustomCameraClose");

        _cutsceneController.StartDialogue(
            _mate,
            [new DialogueLine(DialogueSpeaker.Mate, string.Empty, 3.5f)],
            cinematic: true,
            onFinished: CameraOnClock,
            keepPlayerLockedOnFinish: true,
            referenceCamera: customCamera
        );
    }

    private void CameraOnClock()
    {
        var customCamera = GetNode<Camera3D>("CustomCameraClock");

        _cutsceneController.StartDialogue(
            _clock,
            [new DialogueLine(new DialogueSpeaker(), string.Empty, 1.0f)],
            cinematic: true,
            onFinished: RunClockTimelapse,
            keepPlayerLockedOnFinish: true,
            referenceCamera: customCamera
        );
    }

    private async void RunClockTimelapse()
    {
        _teacher2.Visible = true;
        _teacher.Visible = false;
        var customCamera = GetNode<Camera3D>("CustomCameraEnd");

        _clock.StartTimelapse(20.5f, 3.0f);

        await ToSignal(GetTree().CreateTimer(3.0f), SceneTreeTimer.SignalName.Timeout);

        _cutsceneController.StartDialogue(
            _teacher2,
            [new DialogueLine(DialogueSpeaker.PolishTeacher, PolishClassText.EndOfClass, 3.5f)],
            cinematic: true,
            referenceCamera: customCamera,
            onFinished: EndScene,
            keepPlayerLockedOnFinish: true
        );
    }

    private async void EndScene()
    {
        await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);
        SceneTransition.Instance.ChangeScene("res://Scenes/Levels/PolishClass/polish_class_after.tscn");
    }
}