using Godot;

public partial class Downstairs : LevelBase
{
    private Door _ceilingDoor;
    private CutsceneController _cutsceneController;
    private Player _player;
    private Camera3D _camera;

    public override void _Ready()
    {
        base._Ready();

        _cutsceneController = GetNode<CutsceneController>("CutsceneController");
        _player = GetNode<Player>("Player");
        _camera = _player.GetNode<Camera3D>("Camera3D");
        _cutsceneController.SetCamera(_camera);
        _cutsceneController.SetPlayer(_player);

        _ceilingDoor = GetNode<Door>("CeilingDoor");
        _ceilingDoor.InteractionEnabled = true;
        _ceilingDoor.Interacted += OnCeilingDoorInteracted;
    }

    private void OnCeilingDoorInteracted()
    {
        _cutsceneController.StartDialogue(
            _player,
            [new DialogueLine(DialogueSpeaker.Player, DownstairsText.CelingDoorInteraction, 2.0f)]
        );
    }
}
