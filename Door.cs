using Godot;
using System;

public partial class Door : AnimatableBody3D
{
	private Node3D _player;
	private Node3D _hinge;
	private bool _isOpen = false;
	private float _targetRotation = 0.0f;
	
	public override void _Ready()
	{
		_player = GetTree().GetFirstNodeInGroup("player") as Node3D;
		_hinge = GetNode<Node3D>("Hinge");
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("interact"))
		{
			float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);

			if (distance < 2.0f)
			{
				_isOpen = !_isOpen;
				_targetRotation = _isOpen ? 90.0f : 0.0f;
			}
		}

		float currentRotation = _hinge.RotationDegrees.Y;

		float newRotation = Mathf.MoveToward(
			currentRotation,
			_targetRotation,
			180.0f * (float)delta
		);

		_hinge.RotationDegrees = new Vector3(
			0,
			newRotation,
			0
		);
	}
}
