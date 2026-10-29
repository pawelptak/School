using Godot;

public partial class DownstairsFight : LevelBase
{
    private Door _ceilingDoor;
    private CutsceneController _cutsceneController;
    private Player _player;
    private Camera3D _camera;
    private Bully2 _bully;
    private NpcHealthController _bullyHealthController;

    private NpcController _bullyController;

    private BullyMinion1 _lewyKrzys;
    private NpcHealthController _lewyKrzysHealthController;

    private BullyMinion2 _prawyKrzys;
    private NpcHealthController _prawyKrzysHealthController;
    private NpcController _lewyKrzysController;
    private NpcController _prawyKrzysController;

    private Mate _mate;
    private NpcController _mateController;
    private int _enemyDefeatedCount = 0;

    private Door _germanDoor;

    private Marker3D _mateGoToMarker;

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

        _bully = GetNode<Bully2>("Bully2");
        _bullyController = _bully.GetNode<NpcController>("NpcController");
        _bullyHealthController = _bully.GetNode<NpcHealthController>("NpcHealthController");
        _bullyHealthController.Died += () => _enemyDefeatedCount++;

        _lewyKrzys = GetNode<BullyMinion1>("LewyKrzys");
        _lewyKrzysController = _lewyKrzys.GetNode<NpcController>("NpcController");
        _lewyKrzysHealthController = _lewyKrzys.GetNode<NpcHealthController>("NpcHealthController");
        _lewyKrzysHealthController.Died += () => _enemyDefeatedCount++;

        _prawyKrzys = GetNode<BullyMinion2>("PrawyKrzys");
        _prawyKrzysController = _prawyKrzys.GetNode<NpcController>("NpcController");
        _prawyKrzysHealthController = _prawyKrzys.GetNode<NpcHealthController>("NpcHealthController");
        _prawyKrzysHealthController.Died += () => _enemyDefeatedCount++;


        _bullyController.StartChase(_player);
        _lewyKrzysController.StartChase(_player);
        _prawyKrzysController.StartChase(_player);

        _mate = GetNode<Mate>("Mate");
        _mateController = _mate.GetNode<NpcController>("NpcController");
        _mateController.MoveToFinished += StartMateDialogue;

        _germanDoor = GetNode<Door>("GermanDoor");
        _germanDoor.Interacted += ChangeScene;

        _mateGoToMarker = GetNode<Marker3D>("MateGoToMarker");
    }

    public override void _Process(double delta)
    {
        if (_enemyDefeatedCount == 3)
        {
            _enemyDefeatedCount++;
            _mate.Visible = true;
            _mateController.MoveTo(_player, stopDistance: 1.0f);
        }
    }

    private void StartMateDialogue()
    {
        _cutsceneController.StartDialogue(
            _mate,
            [new DialogueLine(DialogueSpeaker.Mate, DownstairsFightText.MateAfterFight1, 1.0f),
             new DialogueLine(DialogueSpeaker.Mate, DownstairsFightText.MateAfterFight2, 3.0f),
             new DialogueLine(DialogueSpeaker.Mate, DownstairsFightText.MateAfterFight3, 3.0f),
             new DialogueLine(DialogueSpeaker.Mate, DownstairsFightText.MateAfterFight4, 3.0f)],
            cinematic: true,
            onFinished: MateGoToDoor
        );

        _germanDoor.InteractionEnabled = true;
    }

    private void MateGoToDoor()
    {
        _mateController.MoveToFinished -= StartMateDialogue;
        _mateController.MoveToFinished += MateDisappear;
        _mateController.MoveTo(_mateGoToMarker);
    }

    private void MateDisappear()
    {
        _mate.QueueFree();
    }

    private void OnCeilingDoorInteracted()
    {
        _cutsceneController.StartDialogue(
            _player,
            [new DialogueLine(DialogueSpeaker.Player, DownstairsText.CelingDoorInteraction, 2.0f)]
        );
    }

    private void ChangeScene()
    {
        SceneTransition.Instance.ChangeScene("res://Scenes/Levels/DemoEndScreen/demo_end_screen.tscn", duration: 2.0f);
    }
}
