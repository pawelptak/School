using Godot;
using System.Collections.Generic;

public partial class CorridorFight1 : LevelBase
{
    private Mate _mate;
    private NpcController _mateController;
    private Bully _bully;
    private NpcController _bullyController;
    private NpcHealthController _bullyHealthController;
    private CutsceneController _cutsceneController;
    private ObjectiveMarker _objectiveMarker;
    private ThrowableObject _deadBullyBody;

    private Player _player;
    private Camera3D _camera;

    private Marker3D _mateExitPoint;
    private Popup _popup;

    private Door _menDoor;
    private Door _womenDoor;

    private Janitor _janitor;

    private bool _toiletDialoguePlayed;

    public override void _Ready()
    {
        base._Ready();

        _mate = GetNode<Mate>("Mate");
        _mateController = _mate.GetNode<NpcController>("NpcController");
        _mateController.MoveToFinished += HideMate;

        _cutsceneController = GetNode<CutsceneController>("CutsceneController");
        _objectiveMarker = GetNode<ObjectiveMarker>("ObjectiveMarker");
        _objectiveMarker.Hide();
        _mateExitPoint = GetNode<Marker3D>("ExitMarker");

        _bully = GetNode<Bully>("Bully");
        _bully.ThrowableBodyCreated += OnBullyBodyCreated;
        _bullyController = _bully.GetNode<NpcController>("NpcController");
        _bullyHealthController = _bully.GetNode<NpcHealthController>("NpcHealthController");
        _bullyHealthController.Died += OnBullyDefeated;
        _bullyController.MoveToFinished += StartBullyDialogue;

        _janitor = GetNode<Janitor>("Janitor");

        _player = GetNode<Player>("Player");
        _player.ObjectPickedUp += OnObjectPickedUp;

        _camera = _player.GetNode<Camera3D>("Camera3D");

        _cutsceneController.SetCamera(_camera);
        _cutsceneController.SetPlayer(_player);

        _popup = GetNode<Popup>("Popup");
        _popup.PopupClosed += StartFight;
        // GameManager.Instance.CurrentSaveData.CanAttack = true;

        _menDoor = GetNode<Door>("MenBathroomDoor");
        _womenDoor = GetNode<Door>("WomenBathroomDoor");

        _menDoor.InteractionEnabled = false;
        _menDoor.Interacted += () => OnDoorInteracted(_menDoor);


        _womenDoor.InteractionEnabled = false;
        _womenDoor.Interacted += () => OnDoorInteracted(_womenDoor);

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

    private void HideMate()
    {
        _mate.QueueFree();
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

    private async void StartFight()
    {
        _bullyController.StartChase(_player);

        await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);

        _cutsceneController.StartDialogue(
            _janitor,
            [
                new DialogueLine(DialogueSpeaker.Janitor, CorridorFight1Text.JanitorShout, 2.0f)
            ]
        );

    }

    private void OnBullyDefeated()
    {
        _cutsceneController.StartDialogue(
            _player,
            [
                new DialogueLine(DialogueSpeaker.Player, "Uff...", 1.0f)
                //new DialogueLine(DialogueSpeaker.Player, "Nie wiedziałem, że tak potrafię.", 3.0f),
                //new DialogueLine(DialogueSpeaker.Player, "Ok, muszę załatwić sobie strój na WF.", 3.0f),
                //new DialogueLine(DialogueSpeaker.Player, "Hmm", 1.0f),
                //new DialogueLine(DialogueSpeaker.Player, "No w sumie on jest na sportowo.", 1.0f)
            ],
            onFinished: SetMarkerToBody
        );
    }

    private void SetMarkerToBody()
    {
        _objectiveMarker.SetTarget(_deadBullyBody);
        _objectiveMarker.Show();
    }

    private void OnBullyBodyCreated(ThrowableObject deadBody)
    {
        _deadBullyBody = deadBody;
    }

    private void OnObjectPickedUp(ThrowableObject objectPickedUp)
    {
        if (objectPickedUp != _deadBullyBody)
            return;

        _objectiveMarker.Hide();

        List<DialogueLine> lines = new();

        if (!_toiletDialoguePlayed)
        {
            lines.Add(new DialogueLine(DialogueSpeaker.Player, "Nie będę się na korytarzu przebierał.", 3.0f));
            _toiletDialoguePlayed = true;
        }

        lines.Add(new DialogueLine(DialogueSpeaker.Player, "Muszę go zanieść do kibla.", 3.0f));

        _cutsceneController.StartDialogue(_player, lines.ToArray(), onFinished: SetMarkerToToilet);
    }

    private void SetMarkerToToilet()
    {
        var toilet = _menDoor;
        _objectiveMarker.SetTarget(toilet);
        _objectiveMarker.Show();

        _menDoor.InteractionEnabled = true;
        _womenDoor.InteractionEnabled = true;
    }

    private void OnDoorInteracted(Door door)
    {
        if (_deadBullyBody == null ||
            _deadBullyBody.GlobalPosition.DistanceTo(door.GlobalPosition) > 2.0f)
        {
            _cutsceneController.StartDialogue(
                _player,
                [
                    new DialogueLine(
                    DialogueSpeaker.Player,
                    "Bez Łysego nie wchodzę.",
                    2.0f)
                ]
            );

            return;
        }

        var sceneToLoad = door == _menDoor ? "res://Scenes/Levels/Bathroom/bathroom.tscn" : "res://Scenes/Levels/WomenBathroom/women_bathroom.tscn";

        SceneTransition.Instance.ChangeScene(sceneToLoad);
    }
}
