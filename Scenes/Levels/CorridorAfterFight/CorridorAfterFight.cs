using Godot;

public partial class CorridorAfterFight : LevelBase
{
    private ObjectiveMarker _objectiveMarker;
    private StaticBody3D _stairCollision;
    private CutsceneController _cutsceneController;
    private Player _player;
    private Camera3D _camera;
    private CorridorSplash _splash;

    public override void _Ready()
    {
        base._Ready();

        _objectiveMarker = GetNode<ObjectiveMarker>("ObjectiveMarker");
        _stairCollision = GetNode<StaticBody3D>("StairCollision");
        _splash = GetNode<CorridorSplash>("Splash");
        _cutsceneController = GetNode<CutsceneController>("CutsceneController");
        _player = GetNode<Player>("Player");
        _camera = _player.GetNode<Camera3D>("Camera3D");
        _cutsceneController.SetCamera(_camera);
        _cutsceneController.SetPlayer(_player);
        _splash.Interacted += OnSplashInteracted;

        _objectiveMarker.SetTarget(_stairCollision);
    }

    private void OnSplashInteracted()
    {
        _cutsceneController.StartDialogue(
            _player,
            [new DialogueLine(DialogueSpeaker.Player, CorridorAfterFightText.SplashInvestigated, 3.0f)]
        );
    }
}
