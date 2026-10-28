using Godot;

public partial class DownstairsFightIntro : LevelBase
{
    private Door _ceilingDoor;
    private CutsceneController _cutsceneController;
    private Player _player;
    private Camera3D _camera;
    private Marker3D _bullyGoToMarker;
    private Marker3D _lewyKrzysGoToMarker;
    private Marker3D _prawyKrzysGoToMarker;
    private Bully2 _bully;
    private NpcController _bullyController;

    private BullyMinion1 _lewyKrzys; 
    private BullyMinion2 _prawyKrzys; 
    private NpcController _lewyKrzysController;
    private NpcController _prawyKrzysController;


    public override async void _Ready()
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

        _bullyGoToMarker = GetNode<Marker3D>("BullyGoToMarker");
        _lewyKrzysGoToMarker = GetNode<Marker3D>("LewyKrzysGoToMarker");
        _prawyKrzysGoToMarker = GetNode<Marker3D>("PrawyKrzysGoToMarker");

        _bully = GetNode<Bully2>("Bully2");
        _bullyController = _bully.GetNode<NpcController>("NpcController");

        _lewyKrzys = GetNode<BullyMinion1>("LewyKrzys");
        _lewyKrzysController = _lewyKrzys.GetNode<NpcController>("NpcController");

        _prawyKrzys = GetNode<BullyMinion2>("PrawyKrzys");
        _prawyKrzysController = _prawyKrzys.GetNode<NpcController>("NpcController");

        _bullyController.MoveToFinished += TalkToPlayer;

        await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);

        _bullyController.MoveTo(_bullyGoToMarker);
    }

    private void OnCeilingDoorInteracted()
    {
        _cutsceneController.StartDialogue(
            _player,
            [new DialogueLine(DialogueSpeaker.Player, DownstairsText.CelingDoorInteraction, 2.0f)]
        );
    }

    private void TalkToPlayer()
    {
        _cutsceneController.StartDialogue(
            _bully,
            [
                new DialogueLine(DialogueSpeaker.Bully, DownstairsFightText.BullyTalksToPlayer, 2.0f),
                new DialogueLine(DialogueSpeaker.Bully, DownstairsFightText.BullyTalksToPlayer2, 1.0f),
                new DialogueLine(DialogueSpeaker.Bully, DownstairsFightText.BullyTalksToPlayer3, 2.0f),
                new DialogueLine(DialogueSpeaker.Bully, DownstairsFightText.BullyTalksToPlayer4, 1.0f)],
            cinematic: true,
            keepPlayerLockedOnFinish: true,
            onFinished: IntroduceLewyKrzysiek
        );
    }

    private void IntroduceLewyKrzysiek()
    {
        var customCamera = GetNode<Camera3D>("CustomCameraLewy");
        _lewyKrzysController.MoveTo(_lewyKrzysGoToMarker);

        _cutsceneController.StartDialogue(
            _bully,
            [
                new DialogueLine(new DialogueSpeaker(), string.Empty, 1.0f)],
            cinematic: true,
            keepPlayerLockedOnFinish: true,
            onFinished: IntroducePrawyKrzysiek,
            referenceCamera: customCamera,
            isSkippable: false
        );
    }

    private void IntroducePrawyKrzysiek()
    {
        var customCamera = GetNode<Camera3D>("CustomCameraLewy");
        _prawyKrzysController.MoveTo(_prawyKrzysGoToMarker);

        _cutsceneController.StartDialogue(
            _bully,
            [
                new DialogueLine(new DialogueSpeaker(), string.Empty, 2.0f)],
            cinematic: true,
            keepPlayerLockedOnFinish: true,
            onFinished: TransitionToFight,
            referenceCamera: customCamera,
            isSkippable: false
        );
    }

    private async void TransitionToFight()
    {
        await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);
        GetNodeOrNull<CanvasLayer>("/root/Hud")?.Show();
        SceneTransition.Instance.ChangeScene("res://Scenes/Levels/DownstairsFight/downstairs_fight.tscn");
    }
}
