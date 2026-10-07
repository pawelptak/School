using Godot;

public partial class ThrowableObject : RigidBody3D
{
	public float ThrowForce { get; set; }

	public override void _Ready()
	{
		ContactMonitor = true;
		MaxContactsReported = 4;

		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node body)
	{
		GD.Print($"Throwable hit: {body.Name}");
	}
}
