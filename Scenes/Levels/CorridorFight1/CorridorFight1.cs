using Godot;

public partial class CorridorFight1 : LevelBase
{
    private Mate _mate;
    private NpcController _mateController;
    private Bully _bully;
    private NpcController _bullyController;
    private CutsceneController _cutsceneController;
    private ObjectiveMarker _objectiveMarker;

    private Player _player;
    private Camera3D _camera;

    private Marker3D _mateExitPoint;
    private Popup _popup;

    public override void _Ready()
    {
        base._Ready();

        _mate = GetNode<Mate>("Mate");
        _mateController = _mate.GetNode<NpcController>("NpcController");
        _cutsceneController = GetNode<CutsceneController>("CutsceneController");
        //_objectiveMarker = GetNode<ObjectiveMarker>("ObjectiveMarker");
        _mateExitPoint = GetNode<Marker3D>("ExitMarker");
        // _objectiveMarker.SetTarget(_mate);

        _bully = GetNode<Bully>("Bully");
        _bullyController = _bully.GetNode<NpcController>("NpcController");
        _bullyController.MoveToFinished += StartBullyDialogue;

        _player = GetNode<Player>("Player");
        _camera = _player.GetNode<Camera3D>("Camera3D");

        _cutsceneController.SetCamera(_camera);
        _cutsceneController.SetPlayer(_player);

        _popup = GetNode<Popup>("Popup");
        _popup.PopupClosed += StartFight;
        // GameManager.Instance.CurrentSaveData.CanAttack = true;

        StartMateDialogue();

    }

    private void StartMateDialogue()
    {
        _cutsceneController.StartDialogue(
            _mate,
            [
                new DialogueLine(DialogueSpeaker.Mate, CorridorFight1Text.GoAwayText, 4.0f),
                new DialogueLine(DialogueSpeaker.Player, CorridorFight1Text.ResponseToMate, 2.0f),
                new DialogueLine(DialogueSpeaker.Mate, CorridorFight1Text.What, 1.0f),
                new DialogueLine(DialogueSpeaker.Player, CorridorFight1Text.ResponseToMate2, 2f),
                new DialogueLine(DialogueSpeaker.Mate, CorridorFight1Text.ResponseToPlayer, 4.5f),
                new DialogueLine(DialogueSpeaker.Mate, CorridorFight1Text.ResponseToPlayer2, 2.0f)
            ],
            onFinished: MateGoAway,
            cinematic: true,
            delaySeconds: 0.5f
        );
    }

    private async void MateGoAway()
    {
        _mateController.MoveTo(_mateExitPoint);
        _bullyController.MoveTo(_player, stopDistance: 1.0f);

        await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);

        _cutsceneController.StartDialogue(
            _bully,
            [
                new DialogueLine(DialogueSpeaker.Bully, CorridorFight1Text.BullyApproach, 1.0f),
            ]
        );
    }

    private void StartBullyDialogue()
    {
        // TODO: bug Bully interaction text does not dissapear
        _cutsceneController.StartDialogue(
            _bully,
            [
                new DialogueLine(DialogueSpeaker.Bully, CorridorFight1Text.BullyRequest, 3.0f),
                new DialogueLine(DialogueSpeaker.Player, CorridorFight1Text.PlayerResponseToBully, 3.0f),
                new DialogueLine(DialogueSpeaker.Bully, CorridorFight1Text.BullyResponse, 3.0f),
                new DialogueLine(DialogueSpeaker.Bully, CorridorFight1Text.BullyResponse2, 2.5f)
            ],
            onFinished: ShowCombatInstructions,
            cinematic: true
        );
    }

    private void ShowCombatInstructions()
    {
        _popup.ShowPopup(CorridorFight1Text.FightTutorialPoupHeader, string.Format(CorridorFight1Text.FightTutorialPoupText, "LPM"));
        GameManager.Instance.CurrentSaveData.AttackingUnlocked = true;

    }

    private void StartFight()
    {
        _bullyController.StartChase(_player);
    }
}
