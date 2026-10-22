using Godot;
using System.Collections.Generic;

public partial class GoatMinigameController : Node
{
    [Export] public Player Player { get; set; }
    [Export] public PackedScene MedicalBallScene { get; set; }
    [Export] public float Duration { get; set; } = 10.0f; // defaults to 60.0
    [Export] public float BallRespawnDelay { get; set; } = 0.25f;

    [ExportGroup("Scoring")]
    [Export] public int Grade6Hits { get; set; } = 40;
    [Export] public int Grade5Hits { get; set; } = 35;
    [Export] public int Grade4Hits { get; set; } = 30;
    [Export] public int Grade3Hits { get; set; } = 20;
    [Export] public int Grade2Hits { get; set; } = 15;

    private int _hits;

    private HeldObjectController _heldObjectController;
    private Node3D _holdPoint;

    private readonly List<ThrowableObject> _spawnedBalls = new();

    private ThrowableObject _currentBall;
    private bool _minigameActive;

    [Signal]
    public delegate void MinigameFinishedEventHandler(int hits, int grade);

    public override void _Ready()
    {
        if (Player == null || MedicalBallScene == null)
        {
            GD.PushError(
                "GoatMinigameController: assign Player and MedicalBallScene."
            );
            return;
        }

        _heldObjectController =
            Player.GetNode<HeldObjectController>("HeldObjectController");

        _holdPoint =
            Player.GetNode<Node3D>("Camera3D/HoldPoint");

        _heldObjectController.ObjectThrown += OnObjectThrown;

        CallDeferred(nameof(StartMinigame));
    }

    private async void StartMinigame()
    {
        if (_heldObjectController.HeldObject != null)
        {
            GD.PushWarning(
                "Minigame requires the player's hands to be empty."
            );
            return;
        }

        _minigameActive = true;

        if (!SpawnBall())
        {
            _minigameActive = false;
            return;
        }

        await ToSignal(
            GetTree().CreateTimer(Duration),
            SceneTreeTimer.SignalName.Timeout
        );

        FinishGame();
    }

    private bool SpawnBall()
    {
        if (!_minigameActive
            || _currentBall != null
            || _heldObjectController.HeldObject != null)
        {
            return false;
        }

        var ball = MedicalBallScene.Instantiate<ThrowableObject>();
        ball.HitSomething += OnBallHitSomething;

        GetTree().CurrentScene.AddChild(ball);
        ball.GlobalTransform = _holdPoint.GlobalTransform;

        if (!Player.TryPickup(ball))
        {
            ball.QueueFree();
            return false;
        }

        _currentBall = ball;
        _spawnedBalls.Add(ball);

        return true;
    }

    private void OnBallHitSomething(ThrowableObject ball, Node body)
    {
        if (!_minigameActive)
            return;

        if (body is not Goat goat)
            return;

        ball.HitSomething -= OnBallHitSomething;

        _hits++;
        goat.RegisterHit();

        GD.Print($"Hits: {_hits}");
    }

    private async void ReplenishBallAfterCollision(ThrowableObject ball)
    {
        await ToSignal(
            GetTree().CreateTimer(BallRespawnDelay),
            SceneTreeTimer.SignalName.Timeout
        );

        if (!_minigameActive)
            return;

        if (GodotObject.IsInstanceValid(ball))
            ball.QueueFree();

        _spawnedBalls.Remove(ball);

        SpawnBall();
    }

    private void OnObjectThrown(ThrowableObject thrownObject)
    {
        if (!_minigameActive
            || !ReferenceEquals(thrownObject, _currentBall))
        {
            return;
        }

        _currentBall = null;
        ReplenishBall();
    }

    private async void ReplenishBall()
    {
        await ToSignal(
            GetTree().CreateTimer(BallRespawnDelay),
            SceneTreeTimer.SignalName.Timeout
        );

        if (!_minigameActive)
            return;

        SpawnBall();
    }

    private void StopMinigame()
    {
        if (!_minigameActive)
            return;

        _minigameActive = false;

        var heldObject = _heldObjectController.HeldObject;

        if (heldObject != null && _spawnedBalls.Contains(heldObject))
            _heldObjectController.DropObject();

        foreach (var ball in _spawnedBalls)
        {
            if (GodotObject.IsInstanceValid(ball))
                ball.QueueFree();
        }

        _spawnedBalls.Clear();
        _currentBall = null;

        EmitSignal("MinigameFinished", _hits, CalculateGrade(_hits));
    }

    public override void _ExitTree()
    {
        if (_heldObjectController != null
            && GodotObject.IsInstanceValid(_heldObjectController))
        {
            _heldObjectController.ObjectThrown -= OnObjectThrown;
        }

        StopMinigame();
    }

    private void FinishGame()
    {
        GD.Print($"End. Hits: {_hits}");

        int grade = CalculateGrade(_hits);

        GameManager.Instance.SaveMinigameResult(
            "goat_minigame",
            _hits,
            grade
        );

        GD.Print($"Grade: {grade}");

        StopMinigame();
    }

    private int CalculateGrade(int hits)
    {
        if (hits >= Grade6Hits)
            return 6;
        if (hits >= Grade5Hits)
            return 5;
        if (hits >= Grade4Hits)
            return 4;
        if (hits >= Grade3Hits)
            return 3;
        if (hits >= Grade2Hits)
            return 2;

        return 1;
    }
}
