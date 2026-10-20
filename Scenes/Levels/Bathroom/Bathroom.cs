using Godot;

public partial class Bathroom : LevelBase
{
    private Door _door;
    private BullyBody _bullyBody;
    private CutsceneController _cutsceneController;
    private Player _player;
    private Camera3D _camera;

    public override void _Ready()
    {
        base._Ready();

        _door = GetNode<Door>("ExitDoor");
        _door.InteractionEnabled = false;
        _door.Interacted += OnDoorInteracted;


        _bullyBody = GetNode<BullyBody>("BullyBody");
        _bullyBody.InteractionEnabled = true;
        _bullyBody.Interacted += OnBodyInteracted;

        _player = GetNode<Player>("Player");
        _cutsceneController = GetNode<CutsceneController>("CutsceneController");
        _camera = _player.GetNode<Camera3D>("Camera3D");

        _cutsceneController.SetCamera(_camera);
        _cutsceneController.SetPlayer(_player);
    }

    private void OnBodyInteracted()
    {

        _door.InteractionEnabled = true;
        _bullyBody.InteractionEnabled = false;
        _cutsceneController.StartDialogue(
            _player,
            [new DialogueLine(DialogueSpeaker.Player, BathroomText.AfterClothesChange, 3.0f)]
        );
    }

    private void OnDoorInteracted()
    {
        SceneTransition.Instance.ChangeScene("res://Scenes/Levels/CorridorAfterFight/corridor_after_fight.tscn");
    }
}
