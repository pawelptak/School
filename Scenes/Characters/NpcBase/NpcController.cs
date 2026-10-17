using Godot;

public partial class NpcController : Node
{
    public enum NpcState
    {
        Patrol,
        Chase,
        Idle,
        MoveTo
    }

    [Signal]
    public delegate void MoveToFinishedEventHandler();

    private Node3D _moveToTarget;
    private float _moveToStopDistance = 0.5f;

    [Export] public Node3D PatrolPointA { get; set; }
    [Export] public Node3D PatrolPointB { get; set; }

    [Export] public float Speed { get; set; } = 3f;
    [Export] public float ArrivalDistance { get; set; } = 0.4f;
    [Export] public float WaitTime { get; set; } = 2.0f;
    [Export] public float AttackDistance { get; set; } = 1.6f;
    [Export] public float AttackCooldown { get; set; } = 1.5f;
    [Export] public float AttackDamage { get; set; } = 10.0f; // 10.0 by default
    [Export] public float KnockbackDeceleration { get; set; } = 12.0f;

    private CharacterBody3D _npc;
    private NavigationAgent3D _agent;
    private SpriteAnimator _spriteAnimator;
    private Node3D _currentTarget;
    private bool _initialized;

    private bool _isWaiting;
    private float _waitTimer;

    private NpcState _currentState = NpcState.Patrol;
    private Node3D _chaseTarget;

    private float _attackTimer = 0.0f;

    private NpcHealthController _healthController;
    private bool _isDefeated;
    private Vector3 _knockbackVelocity;
    private float _stunTimer;
    private bool _isAttacking;

    public override async void _Ready()
    {
        _npc = GetParent() as CharacterBody3D;
        _agent = _npc?.GetNodeOrNull<NavigationAgent3D>("NavigationAgent3D");
        _spriteAnimator = _npc?.GetNodeOrNull<SpriteAnimator>("SpriteAnimator");

        if (_spriteAnimator != null)
        {
            _spriteAnimator.AttackHitFrame += OnAttackHitFrame;
            _spriteAnimator.AttackAnimationFinished += OnAttackAnimationFinished;
        }

        if (_npc == null || _agent == null)
        {
            GD.PushError($"{Name}: Missing references to NPC or Agent!");
            SetPhysicsProcess(false);
            return;
        }

        // Podłączenie sygnału Avoidance
        _agent.VelocityComputed += OnVelocityComputed;

        _agent.PathDesiredDistance = ArrivalDistance;
        _agent.TargetDesiredDistance = ArrivalDistance;

        bool hasPatrolPoints = PatrolPointA != null && PatrolPointB != null;

        _currentState = hasPatrolPoints ? NpcState.Patrol : NpcState.Idle;

        if (!hasPatrolPoints)
        {
            GD.Print($"{Name}: Patrol Points not set, starting in Idle state.");
        }

        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        if (hasPatrolPoints)
        {
            _currentTarget = PatrolPointA;
            _agent.TargetPosition = _currentTarget.GlobalPosition;
            GD.Print($"[DEBUG] Navigation ready. First target: {_currentTarget.GlobalPosition}");
        }

        _initialized = true;

        _healthController = _npc?.GetNodeOrNull<NpcHealthController>("NpcHealthController");

        if (_healthController != null)
        {
            _healthController.Died += OnNpcDied;
        }
        else
        {
            GD.PushWarning($"{Name}: NpcHealthController was not found.");
        }
    }

    public override void _PhysicsProcess(double dt)
    {
        if (!_initialized || _agent == null || _npc == null)
            return;

        if (_attackTimer > 0.0f)
        {
            _attackTimer -= (float)dt;
        }

        if (ProcessHitEffects(dt))
            return;

        switch (_currentState)
        {
            case NpcState.Patrol:
                ProcessPatrol(dt);
                break;

            case NpcState.Chase:
                ProcessChase(dt);
                break;

            case NpcState.Idle:
                ProcessIdle(dt);
                break;

            case NpcState.MoveTo:
                ProcessMoveTo(dt);
                break;
        }
    }

    public void MoveTo(Node3D target, float stopDistance = 0.5f)
    {
        if (_isDefeated || target == null || !GodotObject.IsInstanceValid(target))
            return;

        if (_agent == null || _npc == null)
            return;

        _moveToTarget = target;
        _moveToStopDistance = Mathf.Max(0.1f, stopDistance);

        _isWaiting = false;
        _isAttacking = false;

        _currentState = NpcState.MoveTo;

        _agent.TargetDesiredDistance = _moveToStopDistance;
        _agent.TargetPosition = target.GlobalPosition;

        GD.Print($"[NPC] {_npc.Name} is moving to {target.Name}.");
    }

    private void ProcessMoveTo(double dt)
    {
        if (_moveToTarget == null ||
            !GodotObject.IsInstanceValid(_moveToTarget))
        {
            StopMovingToTarget();
            return;
        }

        _agent.TargetPosition = _moveToTarget.GlobalPosition;

        Vector3 targetFlat = _moveToTarget.GlobalPosition;
        targetFlat.Y = _npc.GlobalPosition.Y;

        float distance = _npc.GlobalPosition.DistanceTo(targetFlat);
        if (distance <= _moveToStopDistance)
        {
            ApplyGravityAndStop(dt);
            _spriteAnimator?.SetMoving(false);

            _currentState = NpcState.Idle;
            _moveToTarget = null;

            _agent.TargetDesiredDistance = ArrivalDistance;

            GD.Print($"[NPC] {_npc.Name} reached its destination.");
            StopMovingToTarget();
            EmitSignal(SignalName.MoveToFinished);

            return;
        }

        MoveTowardTarget(_moveToTarget.GlobalPosition, dt);
    }

    public void StopMovingToTarget()
    {
        if (_npc == null || _agent == null)
            return;

        _moveToTarget = null;
        _currentState = NpcState.Idle;

        _agent.TargetDesiredDistance = ArrivalDistance;

        ApplyGravityAndStop(0);
        _spriteAnimator?.SetMoving(false);
    }

    private void ProcessPatrol(double dt)
    {
        if (_currentTarget == null)
        {
            _currentState = NpcState.Idle;
            return;
        }

        if (_isWaiting)
        {
            _waitTimer -= (float)dt;
            ApplyGravityAndStop(dt);
            _spriteAnimator?.SetMoving(false);

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

        MoveTowardTarget(_currentTarget.GlobalPosition, dt);
    }

    private void ProcessChase(double dt)
    {
        if (_chaseTarget == null ||
            !GodotObject.IsInstanceValid(_chaseTarget))
        {
            return;
        }

        _agent.TargetPosition = _chaseTarget.GlobalPosition;

        Vector3 targetFlat = _chaseTarget.GlobalPosition;
        targetFlat.Y = _npc.GlobalPosition.Y;

        float distance = _npc.GlobalPosition.DistanceTo(targetFlat);

        if (_isAttacking)
        {
            ApplyGravityAndStop(dt);
            _spriteAnimator?.SetMoving(false);
            FaceTarget(targetFlat);
            return;
        }

        if (distance <= AttackDistance)
        {
            ApplyGravityAndStop(dt);
            _spriteAnimator?.SetMoving(false);
            FaceTarget(targetFlat);

            if (_attackTimer <= 0.0f)
            {
                if (_spriteAnimator != null && _spriteAnimator.PlayAttack())
                {
                    _isAttacking = true;
                    _attackTimer = AttackCooldown;
                }
                else
                {
                    _attackTimer = AttackCooldown;
                }
            }

            return;
        }

        MoveTowardTarget(_chaseTarget.GlobalPosition, dt);
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - _npc.GlobalPosition;
        direction.Y = 0.0f;

        if (direction.LengthSquared() < 0.001f)
            return;

        direction = direction.Normalized();

        _npc.Rotation = new Vector3(
            _npc.Rotation.X,
            Mathf.Atan2(direction.X, direction.Z),
            _npc.Rotation.Z
        );
    }

    private void OnAttackHitFrame()
    {
        if (_isDefeated ||
            _npc == null ||
            _chaseTarget == null ||
            !GodotObject.IsInstanceValid(_chaseTarget))
        {
            return;
        }

        Vector3 targetFlat = _chaseTarget.GlobalPosition;
        targetFlat.Y = _npc.GlobalPosition.Y;

        float distance = _npc.GlobalPosition.DistanceTo(targetFlat);

        if (distance > AttackDistance)
        {
            return;
        }

        var healthController =
            _chaseTarget.GetNodeOrNull<HealthController>("HealthController");

        if (healthController != null)
        {
            healthController.TakeDamage(AttackDamage);
        }
    }

    private void OnAttackAnimationFinished()
    {
        _isAttacking = false;
    }

    private void ProcessIdle(double dt)
    {
        ApplyGravityAndStop(dt);
        _spriteAnimator?.SetMoving(false);
    }

    private void MoveTowardTarget(Vector3 targetPos, double dt)
    {
        var path = _agent.GetCurrentNavigationPath();
        Vector3 nextWaypoint = (path.Length >= 2) ? path[1] : targetPos;

        Vector3 direction = nextWaypoint - _npc.GlobalPosition;
        direction.Y = 0;

        bool isMoving = false;
        Vector3 desiredVelocity = _npc.Velocity;

        if (direction.LengthSquared() > 0.001f)
        {
            isMoving = true;
            direction = direction.Normalized();

            desiredVelocity.X = direction.X * Speed;
            desiredVelocity.Z = direction.Z * Speed;

            _npc.Rotation = new Vector3(
                _npc.Rotation.X,
                Mathf.Atan2(direction.X, direction.Z),
                _npc.Rotation.Z
            );
        }
        else
        {
            desiredVelocity.X = 0;
            desiredVelocity.Z = 0;
        }

        if (!_npc.IsOnFloor())
        {
            desiredVelocity.Y -= 9.8f * (float)dt;
        }
        else
        {
            desiredVelocity.Y = -0.1f;
        }

        _spriteAnimator?.SetMoving(isMoving);

        _agent.SetVelocity(desiredVelocity);
    }

    private void ApplyGravityAndStop(double dt)
    {
        Vector3 velocity = _npc.Velocity;
        velocity.X = 0;
        velocity.Z = 0;

        if (!_npc.IsOnFloor())
        {
            velocity.Y -= 9.8f * (float)dt;
        }
        else
        {
            velocity.Y = -0.1f;
        }

        _agent.SetVelocity(velocity);
    }

    private void OnVelocityComputed(Vector3 safeVelocity)
    {
        if (_stunTimer > 0.0f || _knockbackVelocity.LengthSquared() > 0.001f)
            return;

        Vector3 horizontalVelocity = new Vector3(safeVelocity.X, 0, safeVelocity.Z);
        float maxAllowedSpeed = Speed * 1.2f;
        if (horizontalVelocity.Length() > maxAllowedSpeed)
        {
            horizontalVelocity = horizontalVelocity.Normalized() * maxAllowedSpeed;
        }

        safeVelocity.X = horizontalVelocity.X;
        safeVelocity.Z = horizontalVelocity.Z;

        _npc.Velocity = safeVelocity;
        _npc.MoveAndSlide();
    }

    private void SwitchTarget()
    {
        if (PatrolPointA == null || PatrolPointB == null)
        {
            return;
        }

        _currentTarget = (_currentTarget == PatrolPointA) ? PatrolPointB : PatrolPointA;
        _agent.TargetPosition = _currentTarget.GlobalPosition;
        GD.Print($"[PATROL] Switching target to: {_currentTarget.Name}");
    }

    public void StartChase(Node3D target)
    {
        if (_isDefeated)
            return;

        _currentState = NpcState.Chase;
        _chaseTarget = target;
        _attackTimer = 0.0f;
        GD.Print($"[COMBAT] {_npc.Name} started chasing {target.Name}.");
    }

    public void SetIdleState()
    {
        _currentState = NpcState.Idle;
        GD.Print($"[COMBAT] {_npc.Name} switched to Idle.");
    }

    public void ResumePatrol()
    {
        if (_isDefeated)
            return;

        if (_currentTarget == null)
        {
            if (PatrolPointA == null || PatrolPointB == null)
            {
                _currentState = NpcState.Idle;
                GD.Print($"{Name}: Cannot resume patrol, Patrol Points not set. Switching to Idle.");
                return;
            }

            _currentTarget = PatrolPointA;
        }

        _currentState = NpcState.Patrol;
        _agent.TargetPosition = _currentTarget.GlobalPosition;
        GD.Print($"[PATROL] {_npc.Name} resumed patrolling.");
    }

    private void OnNpcDied()
    {
        if (_isDefeated)
            return;

        _isDefeated = true;
        _currentState = NpcState.Idle;
        _chaseTarget = null;
        _isWaiting = false;
        _knockbackVelocity = Vector3.Zero;
        _stunTimer = 0.0f;

        _spriteAnimator?.SetMoving(false);
        SetPhysicsProcess(false);

        GD.Print($"[COMBAT] {_npc.Name} was defeated.");
    }

    public override void _ExitTree()
    {
        if (_agent != null)
        {
            _agent.VelocityComputed -= OnVelocityComputed;
        }

        if (_healthController != null &&
            GodotObject.IsInstanceValid(_healthController))
        {
            _healthController.Died -= OnNpcDied;
        }

        if (_spriteAnimator != null &&
            GodotObject.IsInstanceValid(_spriteAnimator))
        {
            _spriteAnimator.AttackHitFrame -= OnAttackHitFrame;
            _spriteAnimator.AttackAnimationFinished -= OnAttackAnimationFinished;
        }
    }

    public void ApplyHitEffects(
        ThrowableObject throwable,
        float knockback,
        float stunDuration)
    {
        if (throwable == null)
            return;

        Vector3 direction = _npc.GlobalPosition - throwable.GlobalPosition;
        ApplyKnockback(direction, knockback, stunDuration);
    }

    public void ApplyKnockback(
        Vector3 direction,
        float knockback,
        float stunDuration = 0.0f)
    {
        if (_isDefeated || _npc == null)
            return;

        direction.Y = 0.0f;

        if (direction.LengthSquared() > 0.001f && knockback > 0.0f)
        {
            direction = direction.Normalized();
            _knockbackVelocity += direction * knockback;
        }

        _stunTimer = Mathf.Max(_stunTimer, stunDuration);
    }

    private bool ProcessHitEffects(double dt)
    {
        if (_npc == null)
            return false;

        bool isStunned = _stunTimer > 0.0f;

        if (isStunned)
            _stunTimer = Mathf.Max(0.0f, _stunTimer - (float)dt);

        _knockbackVelocity = _knockbackVelocity.MoveToward(
            Vector3.Zero,
            KnockbackDeceleration * (float)dt
        );

        Vector3 velocity = _npc.Velocity;

        velocity.X = _knockbackVelocity.X;
        velocity.Z = _knockbackVelocity.Z;

        if (!_npc.IsOnFloor())
            velocity.Y -= 9.8f * (float)dt;
        else if (velocity.Y < 0.0f)
            velocity.Y = -0.1f;

        _npc.Velocity = velocity;
        _npc.MoveAndSlide();

        bool isBeingKnockedBack = _knockbackVelocity.LengthSquared() > 0.01f;

        if (!isStunned && !isBeingKnockedBack)
        {
            _knockbackVelocity = Vector3.Zero;
            return false;
        }

        _spriteAnimator?.SetMoving(false);
        return true;
    }

    public void RecoverFromDefeat()
    {
        if (_npc == null || _agent == null)
            return;

        _isDefeated = false;
        _knockbackVelocity = Vector3.Zero;
        _stunTimer = 0.0f;
        _chaseTarget = null;
        _isWaiting = false;
        _waitTimer = 0.0f;
        _attackTimer = 0.0f;
        _isAttacking = false;

        if (_currentTarget == null)
        {
            _currentTarget = PatrolPointA;
        }

        if (_currentTarget != null)
        {
            _currentState = NpcState.Patrol;
            _agent.TargetPosition = _currentTarget.GlobalPosition;
            GD.Print($"[COMBAT] {_npc.Name} recovered and resumed patrol.");
        }
        else
        {
            _currentState = NpcState.Idle;
            GD.Print($"[COMBAT] {_npc.Name} recovered and switched to Idle (no Patrol Points).");
        }

        _spriteAnimator?.SetMoving(false);
        _spriteAnimator?.RecoverFromDead();

        SetPhysicsProcess(true);
    }
}