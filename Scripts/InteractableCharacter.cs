using Godot;
using System;

public partial class InteractableCharacter : CharacterBody3D
{
	protected bool PlayerInRange { get; private set; }

	public event Action<float> HitByThrowable;

	public override void _Ready()
	{
		var interactionArea = GetNode<Area3D>("InteractionArea");

		interactionArea.BodyEntered += OnBodyEntered;
		interactionArea.BodyExited += OnBodyExited;
	}

	public override void _Process(double delta)
	{
		if (PlayerInRange && Input.IsActionJustPressed("interact"))
		{
			OnInteraction();
		}
	}

	protected virtual void OnInteraction()
	{
	}

	public void NotifyHitByThrowable(float throwForce)
	{
		HitByThrowable?.Invoke(throwForce);
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is Player)
		{
			PlayerInRange = true;
		}
	}

	private void OnBodyExited(Node3D body)
	{
		if (body is Player)
		{
			PlayerInRange = false;
		}
	}
}
