
using Godot;

public partial class Goat : CharacterBody3D
{
    [Signal]
    public delegate void HitEventHandler();

    [ExportGroup("References")]
    [Export]
    public Node3D Player { get; set; }

    [Export]
    public Node3D ArenaCenter { get; set; }

    [ExportGroup("Movement")]
    [Export]
    public float Speed { get; set; } = 5.0f;

    [Export]
    public float Acceleration { get; set; } = 12.0f;

    [Export]
    public float JumpVelocity { get; set; } = 4.5f;

    [Export]
    public float EscapeDistance { get; set; } = 7.0f;

    [Export]
    public float DodgeStrength { get; set; } = 0.8f;

    [ExportGroup("Arena")]
    // Half-size of the arena in X and Z, in meters.
    [Export]
    public Vector2 ArenaHalfSize { get; set; } = new(8.0f, 6.0f);

    [Export]
    public float BoundaryMargin { get; set; } = 2.0f;

    [ExportGroup("Unpredictability")]
    [Export]
    public float DirectionChangeMin { get; set; } = 0.8f;

    [Export]
    public float DirectionChangeMax { get; set; } = 1.8f;

    [Export]
    public float RandomMovementStrength { get; set; } = 0.45f;

    private readonly RandomNumberGenerator _rng = new();

    private Vector3 _randomDirection;
    private float _directionChangeTimer;
    private float _dodgeTimer;
    private float _dodgeDirection = 1.0f;

    public override void _Ready()
    {
        _rng.Randomize();
        PickRandomDirection();
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        if (!IsOnFloor())
        {
            Velocity += GetGravity() * dt;
        }

        if (Player == null || !GodotObject.IsInstanceValid(Player))
        {
            StopHorizontalMovement(dt);
            MoveAndSlide();
            return;
        }

        _directionChangeTimer -= dt;
        _dodgeTimer -= dt;

        if (_directionChangeTimer <= 0.0f)
        {
            PickRandomDirection();
        }

        Vector3 toPlayer = Player.GlobalPosition - GlobalPosition;
        toPlayer.Y = 0.0f;

        float distance = toPlayer.Length();

        Vector3 fleeDirection = Vector3.Zero;

        if (distance > 0.01f)
        {
            Vector3 towardPlayer = toPlayer / distance;

            // Run away from the player.
            fleeDirection = -towardPlayer;

            // Add a sideways movement to make aiming harder.
            Vector3 sideways = new(
                -towardPlayer.Z,
                0.0f,
                towardPlayer.X
            );

            fleeDirection += sideways * DodgeStrength * _dodgeDirection;

            // When the player gets close, make a sudden evasive move.
            if (distance < EscapeDistance && _dodgeTimer <= 0.0f)
            {
                _dodgeDirection = _rng.Randf() < 0.5f ? -1.0f : 1.0f;
                _dodgeTimer = _rng.RandfRange(0.35f, 0.8f);

                fleeDirection += sideways * _dodgeDirection * 1.5f;
            }
        }

        Vector3 direction = fleeDirection + _randomDirection * RandomMovementStrength;

        if (ArenaCenter != null)
        {
            direction += GetBoundaryCorrection();
        }

        direction.Y = 0.0f;

        if (direction.LengthSquared() > 0.001f)
        {
            direction = direction.Normalized();

            Velocity = new Vector3(
                Mathf.MoveToward(Velocity.X, direction.X * Speed, Acceleration * dt),
                Velocity.Y,
                Mathf.MoveToward(Velocity.Z, direction.Z * Speed, Acceleration * dt)
            );

            // Face the direction of travel.
            if (new Vector2(Velocity.X, Velocity.Z).LengthSquared() > 0.1f)
            {
                float targetRotation = Mathf.Atan2(-Velocity.X, -Velocity.Z);

                Rotation = new Vector3(
                    Rotation.X,
                    Mathf.LerpAngle(Rotation.Y, targetRotation, 5.0f * dt),
                    Rotation.Z
                );
            }
        }
        else
        {
            StopHorizontalMovement(dt);
        }

        MoveAndSlide();
    }

    private void PickRandomDirection()
    {
        _randomDirection = new Vector3(
            _rng.RandfRange(-1.0f, 1.0f),
            0.0f,
            _rng.RandfRange(-1.0f, 1.0f)
        ).Normalized();

        _directionChangeTimer = _rng.RandfRange(
            DirectionChangeMin,
            DirectionChangeMax
        );
    }

    private Vector3 GetBoundaryCorrection()
    {
        Vector3 offset = GlobalPosition - ArenaCenter.GlobalPosition;
        Vector3 correction = Vector3.Zero;

        float limitX = Mathf.Max(0.0f, ArenaHalfSize.X - BoundaryMargin);
        float limitZ = Mathf.Max(0.0f, ArenaHalfSize.Y - BoundaryMargin);

        if (offset.X > limitX)
        {
            correction.X = -1.0f;
        }
        else if (offset.X < -limitX)
        {
            correction.X = 1.0f;
        }

        if (offset.Z > limitZ)
        {
            correction.Z = -1.0f;
        }
        else if (offset.Z < -limitZ)
        {
            correction.Z = 1.0f;
        }

        return correction * 3.0f;
    }

    private void StopHorizontalMovement(float dt)
    {
        Velocity = new Vector3(
            Mathf.MoveToward(Velocity.X, 0.0f, Acceleration * dt),
            Velocity.Y,
            Mathf.MoveToward(Velocity.Z, 0.0f, Acceleration * dt)
        );
    }

    // Call this when a medical ball hits the goat.
    public void RegisterHit()
    {
        EmitSignal(SignalName.Hit);
    }
}
