using Godot;

public partial class NpcPatrol : Node
{
    [Export] public Node3D PatrolPointA { get; set; }
    [Export] public Node3D PatrolPointB { get; set; }

    [Export] public float Speed { get; set; } = 1.5f;
    [Export] public float ArrivalDistance { get; set; } = 0.4f;

    private CharacterBody3D _npc;
    private NavigationAgent3D _agent;
    private Node3D _currentTarget;
    private bool _initialized;

    public override async void _Ready()
    {
        _npc = GetParent() as CharacterBody3D;
        _agent = _npc?.GetNodeOrNull<NavigationAgent3D>("NavigationAgent3D");

        if (_npc == null || _agent == null || PatrolPointA == null || PatrolPointB == null)
        {
            GD.PushError($"{Name}: Brak referencji do NPC, Agenta lub Punktów Patrolu!");
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

    }

    public override void _PhysicsProcess(double dt)
    {
        if (!_initialized || _agent == null || _npc == null || _currentTarget == null)
            return;

        Vector3 targetFlat = _currentTarget.GlobalPosition;
        targetFlat.Y = _npc.GlobalPosition.Y;

        if (_npc.GlobalPosition.DistanceTo(targetFlat) <= ArrivalDistance)
        {
            SwitchTarget();
            return;
        }

        var path = _agent.GetCurrentNavigationPath();

        Vector3 nextWaypoint = (path.Length >= 2) ? path[1] : _currentTarget.GlobalPosition;

        Vector3 direction = nextWaypoint - _npc.GlobalPosition;
        direction.Y = 0;

        if (direction.LengthSquared() > 0.001f)
        {
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

        _npc.MoveAndSlide();
    }

    private void SwitchTarget()
    {
        _currentTarget = (_currentTarget == PatrolPointA) ? PatrolPointB : PatrolPointA;
        _agent.TargetPosition = _currentTarget.GlobalPosition;
        GD.Print($"[PATROL] Cel osiągnięty! Zmiana na: {_currentTarget.Name}");
    }
}