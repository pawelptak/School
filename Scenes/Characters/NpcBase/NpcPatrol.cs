using Godot;

public partial class NpcPatrol : Node
{
    [Export] public Node3D PatrolPointA { get; set; }
    [Export] public Node3D PatrolPointB { get; set; }

    [Export] public float Speed { get; set; } = 1.5f;
    [Export] public float ArrivalDistance { get; set; } = 0.4f;
    [Export] public float WaitTime { get; set; } = 2.0f;

    private CharacterBody3D _npc;
    private NavigationAgent3D _agent;
    private SpriteAnimator _spriteAnimator;
    private Node3D _currentTarget;
    private bool _initialized;

    private bool _isWaiting;
    private float _waitTimer;

    public override async void _Ready()
    {
        _npc = GetParent() as CharacterBody3D;
        _agent = _npc?.GetNodeOrNull<NavigationAgent3D>("NavigationAgent3D");
        _spriteAnimator = _npc?.GetNodeOrNull<SpriteAnimator>("SpriteAnimator");

        if (_npc == null || _agent == null || PatrolPointA == null || PatrolPointB == null)
        {
            GD.PushError($"{Name}: Missing references to NPC, Agent, or Patrol Points!");
            SetPhysicsProcess(false);
            return;
        }

        _agent.PathDesiredDistance = ArrivalDistance;
        _agent.TargetDesiredDistance = ArrivalDistance;

        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        _currentTarget = PatrolPointA;
        _agent.TargetPosition = _currentTarget.GlobalPosition;
        _initialized = true;

        GD.Print($"[DEBUG] Navigation ready. First target: {_currentTarget.GlobalPosition}");
    }

    public override void _PhysicsProcess(double dt)
    {
        if (!_initialized || _agent == null || _npc == null || _currentTarget == null)
            return;

        if (_isWaiting)
        {
            _waitTimer -= (float)dt;

            Vector3 waitVelocity = _npc.Velocity;
            waitVelocity.X = 0;
            waitVelocity.Z = 0;

            if (!_npc.IsOnFloor())
            {
                waitVelocity.Y -= 9.8f * (float)dt;
            }
            else
            {
                waitVelocity.Y = -0.1f;
            }

            _npc.Velocity = waitVelocity;
            _spriteAnimator?.SetMoving(false);
            _npc.MoveAndSlide();

            if (_waitTimer <= 0.0f)
            {
                _isWaiting = false;
                SwitchTarget();
            }
            return;
        }

        Vector3 targetFlat = _currentTarget.GlobalPosition;
        targetFlat.Y = _npc.GlobalPosition.Y;

        if (_npc.GlobalPosition.DistanceTo(targetFlat) <= ArrivalDistance)
        {
            _isWaiting = true;
            _waitTimer = WaitTime;
            GD.Print($"[PATROL] Target reached: {_currentTarget.Name}. Waiting for {_waitTimer}s.");
            return;
        }

        var path = _agent.GetCurrentNavigationPath();
        Vector3 nextWaypoint = (path.Length >= 2) ? path[1] : _currentTarget.GlobalPosition;

        Vector3 direction = nextWaypoint - _npc.GlobalPosition;
        direction.Y = 0;

        bool isMoving = false;

        if (direction.LengthSquared() > 0.001f)
        {
            isMoving = true;
            direction = direction.Normalized();

            Vector3 velocity = _npc.Velocity;
            velocity.X = direction.X * Speed;
            velocity.Z = direction.Z * Speed;

            if (!_npc.IsOnFloor())
            {
                velocity.Y -= 9.8f * (float)dt;
            }
            else
            {
                velocity.Y = -0.1f;
            }

            _npc.Velocity = velocity;

            _npc.Rotation = new Vector3(
                _npc.Rotation.X,
                Mathf.Atan2(direction.X, direction.Z),
                _npc.Rotation.Z
            );
        }
        else
        {
            Vector3 velocity = _npc.Velocity;
            velocity.X = 0;
            velocity.Z = 0;
            _npc.Velocity = velocity;
        }

        _spriteAnimator?.SetMoving(isMoving);

        _npc.MoveAndSlide();
    }

    private void SwitchTarget()
    {
        _currentTarget = (_currentTarget == PatrolPointA) ? PatrolPointB : PatrolPointA;
        _agent.TargetPosition = _currentTarget.GlobalPosition;
        GD.Print($"[PATROL] Switching target to: {_currentTarget.Name}");
    }
}