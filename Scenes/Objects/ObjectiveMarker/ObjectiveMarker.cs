using Godot;

public partial class ObjectiveMarker : Node3D
{
    [Export]
    public Node3D Target { get; set; }

    [Export]
    public Vector3 TargetOffset { get; set; } = new(0, 1.5f, 0);

    private Node3D _arrowHead;
    private float _time;

    private const float MovementHeight = 0.15f;
    private const float MovementSpeed = 2.5f;

    public override void _Ready()
    {
        _arrowHead = GetNode<Node3D>("ArrowHead");
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;

        var offset = Mathf.Sin(_time * MovementSpeed) * MovementHeight;

        _arrowHead.Position = new Vector3(
            0,
            -0.375f + offset,
            0
        );

        if (GodotObject.IsInstanceValid(Target))
        {
            GlobalPosition = Target.GlobalTransform * TargetOffset;
        }
    }

    public void SetTarget(Node3D target)
    {
        Target = target;
    }
}
