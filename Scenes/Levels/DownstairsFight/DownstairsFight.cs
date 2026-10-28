using Godot;

public partial class DownstairsFight : LevelBase
{
    private Door _ceilingDoor;
    private CutsceneController _cutsceneController;
    private Player _player;
    private Camera3D _camera;
    private Bully2 _bully;
    private NpcController _bullyController;

    private BullyMinion1 _lewyKrzys;
    private BullyMinion2 _prawyKrzys;
    private NpcController _lewyKrzysController;
    private NpcController _prawyKrzysController;

    public override void _Ready()
    {
        base._Ready();


        _cutsceneController = GetNode<CutsceneController>("CutsceneController");
        _player = GetNode<Player>("Player");
        _camera = _player.GetNode<Camera3D>("Camera3D");
        //_cutsceneController.SetCamera(_camera);
        //_cutsceneController.SetPlayer(_player);

        _ceilingDoor = GetNode<Door>("CeilingDoor");
        _ceilingDoor.InteractionEnabled = true;
        _ceilingDoor.Interacted += OnCeilingDoorInteracted;

        _bully = GetNode<Bully2>("Bully2");
        _bullyController = _bully.GetNode<NpcController>("NpcController");

        _lewyKrzys = GetNode<BullyMinion1>("LewyKrzys");
        _lewyKrzysController = _lewyKrzys.GetNode<NpcController>("NpcController");

        _prawyKrzys = GetNode<BullyMinion2>("PrawyKrzys");
        _prawyKrzysController = _prawyKrzys.GetNode<NpcController>("NpcController");

        //_bullyController.StartChase(_player);
        //_lewyKrzysController.StartChase(_player);
        //_prawyKrzysController.StartChase(_player);
    }

    public override void _Process(double delta)
    {
    }

    private void OnCeilingDoorInteracted()
    {
        _cutsceneController.StartDialogue(
            _player,
            [new DialogueLine(DialogueSpeaker.Player, DownstairsText.CelingDoorInteraction, 2.0f)]
        );
    }
}
