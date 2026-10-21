using Godot;

public partial class GroupOfStudentsFront : CharacterBody3D
{
    public const float Speed = 5.0f;
    public const float JumpVelocity = 4.5f;
    private Vector3 _initialPosition;


    public override void _Ready()
    {
        _initialPosition = GlobalPosition;
    }

    public override void _PhysicsProcess(double delta)
    {
        GlobalPosition = _initialPosition;
        Velocity = Vector3.Zero;

        return;
    }
}
