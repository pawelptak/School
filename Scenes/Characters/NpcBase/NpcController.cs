using Godot;

public partial class NpcController : Node
{
	public enum NpcState
	{
		Patrol,
		Chase,
		Idle
	}

	[Export] public Node3D PatrolPointA { get; set; }
	[Export] public Node3D PatrolPointB { get; set; }

	[Export] public float Speed { get; set; } = 1.5f;
	[Export] public float ArrivalDistance { get; set; } = 0.4f;
	[Export] public float WaitTime { get; set; } = 2.0f;
	[Export] public float AttackDistance { get; set; } = 1.2f;
	[Export] public float AttackCooldown { get; set; } = 1.5f;
	[Export] public float AttackDamage { get; set; } = 50.0f; // 10.0 by default
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

	private float _attackTimer = 0.0f; // Licznik czasu do kolejnego ataku

	private NpcHealthController _healthController;
	private bool _isDefeated;
	private Vector3 _knockbackVelocity;
	private float _stunTimer;

	public override async void _Ready()
	{
		_npc = GetParent() as CharacterBody3D;
		_agent = _npc?.GetNodeOrNull<NavigationAgent3D>("NavigationAgent3D");
		_spriteAnimator = _npc?.GetNodeOrNull<SpriteAnimator>("SpriteAnimator");

		if (_npc == null || _agent == null)
		{
			GD.PushError($"{Name}: Missing references to NPC or Agent!");
			SetPhysicsProcess(false);
			return;
		}

		_agent.PathDesiredDistance = ArrivalDistance;
		_agent.TargetDesiredDistance = ArrivalDistance;

		bool hasPatrolPoints = PatrolPointA != null && PatrolPointB != null;

		// Ustawiamy stan pocz\u0105tkowy synchronicznie, zanim NPC odda kontrol\u0119 (await),
		// aby zewn\u0119trzne wywo\u0142anie StartChase/SetIdleState w tej samej klatce nie zosta\u0142o
		// pó\u017aniej nadpisane przez logik\u0119 inicjalizacji patrolu.
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

		// Aktualizujemy timer ataku w każdej klatce, jeśli jest większy od zera
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
		}
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
		if (_chaseTarget == null) return;

		_agent.TargetPosition = _chaseTarget.GlobalPosition;

		Vector3 targetFlat = _chaseTarget.GlobalPosition;
		targetFlat.Y = _npc.GlobalPosition.Y;

		// Jeśli dotarliśmy na odległość ataku
		if (_npc.GlobalPosition.DistanceTo(targetFlat) <= AttackDistance)
		{
			ApplyGravityAndStop(dt);
			_spriteAnimator?.SetMoving(false);

			// Sprawdzamy czy minął czas cooldownu
			if (_attackTimer <= 0.0f)
			{
				AttackPlayer();
				_attackTimer = AttackCooldown; // Resetujemy licznik
			}

			return;
		}

		MoveTowardTarget(_chaseTarget.GlobalPosition, dt);
	}

	private void AttackPlayer()
	{
		var healthController = _chaseTarget.GetNodeOrNull<HealthController>("HealthController");
		if (healthController != null)
		{
			healthController.TakeDamage(AttackDamage);
			GD.Print($"[COMBAT] {_npc.Name} attacks player for {AttackDamage} damage!");
		}
		else
		{
			GD.PushWarning($"[COMBAT] {_npc.Name} tried to attack, but HealthController was not found on target!");
		}
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
			ApplyGravityAndStop(dt);
		}

		_spriteAnimator?.SetMoving(isMoving);
		_npc.MoveAndSlide();
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

		_npc.Velocity = velocity;
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
		_attackTimer = 0.0f; // Pozwól zaatakować od razu po dobiegnięciu
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
		if (_healthController != null &&
			GodotObject.IsInstanceValid(_healthController))
		{
			_healthController.Died -= OnNpcDied;
		}
	}

	public void ApplyHitEffects(
	ThrowableObject throwable,
	float knockback,
	float stunDuration)
	{
		if (_isDefeated || _npc == null)
			return;

		Vector3 direction = _npc.GlobalPosition - throwable.GlobalPosition;
		direction.Y = 0.0f;

		if (direction.LengthSquared() > 0.001f && knockback > 0.0f)
		{
			direction = direction.Normalized();
			_knockbackVelocity += direction * knockback;
		}

		_stunTimer = Mathf.Max(_stunTimer, stunDuration);

		GD.Print(
			$"[COMBAT] {_npc.Name} hit. " +
			$"Knockback: {knockback}, Stun: {stunDuration}s"
		);
	}

	private bool ProcessHitEffects(double dt)
	{
		if (_npc == null)
			return false;

		bool isStunned = _stunTimer > 0.0f;

		if (isStunned)
			_stunTimer = Mathf.Max(0.0f, _stunTimer - (float)dt);

		// Zmniejszamy odrzut w czasie.
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
