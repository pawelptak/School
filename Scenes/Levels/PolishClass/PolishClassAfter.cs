using Godot;

public partial class PolishClassAfter : Node3D
{
    private Mate _mate;
    private Player _player;
    private Camera3D _camera;
    private Door _door;
    private CutsceneController _cutsceneController;

    public override void _Ready()
    {
        _door = GetNode<Door>("Door");
        _mate = GetNode<Mate>("Mate");
        _player = GetNode<Player>("Player");
        _camera = _player.GetNode<Camera3D>("Camera3D");
        _cutsceneController = GetNode<CutsceneController>("CutsceneController");
        _cutsceneController.SetPlayer(_player);
        _cutsceneController.SetCamera(_camera);

        _door.InteractionEnabled = true;
        _door.Interacted += OnDoorInteracted;

        _cutsceneController.StartDialogue(
            _mate,
            [new DialogueLine(DialogueSpeaker.Mate, MateText.AfterClass, 3.0f)],
            cinematic: true,
            onFinished: ShowGradePopup,
            delaySeconds: 1.0f
        );
    }

    private void OnDoorInteracted()
    {
        SceneTransition.Instance.ChangeScene("res://Scenes/Levels/CorridorFight1/corridor_fight_1.tscn");
    }

    private async void ShowGradePopup()
    {
        // Wait for 1 second before showing the grade popup
        // await ToSignal(GetTree().CreateTimer(1.0f), "timeout");

        _cutsceneController.StartDialogue(
            _mate,
            [new DialogueLine(DialogueSpeaker.Mate, MateText.ExitPolishClass, 2.0f)]
        );
    }
}
