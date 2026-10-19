using Godot;
using System.Collections.Generic;

public partial class DestructionDetector : Node
{
	[Signal]
	public delegate void DestructionThresholdReachedEventHandler();

	[ExportGroup("Detection")]
	[Export] public string ObjectsGroup { get; set; } = "destructible";

	[Export] public float MinLinearSpeed { get; set; } = 2.5f;
	[Export] public float HighLinearSpeed { get; set; } = 5.0f;
	[Export] public float MinAngularSpeed { get; set; } = 5.0f;

	[ExportGroup("Chaos")]
	[Export] public float ChaosThreshold { get; set; } = 5.0f;
	[Export] public float PointsPerEvent { get; set; } = 1.0f;
	[Export] public float HighSpeedBonus { get; set; } = 1.0f;
	[Export] public float PointsDecayPerSecond { get; set; } = 0.5f;
	[ExportGroup("Cooldown")]
	[Export] public float ReactionCooldown { get; set; } = 3.0f;

	private double _cooldownRemaining;

	private readonly Dictionary<ulong, bool> _wasMovingFast = new();

	private float _chaosScore;


	public override void _PhysicsProcess(double delta)
	{
		_cooldownRemaining = Mathf.Max(
			0.0f,
			(float)(_cooldownRemaining - delta)
		);

		_chaosScore = Mathf.Max(
			0.0f,
			_chaosScore - PointsDecayPerSecond * (float)delta
		);

		var objects = GetTree().GetNodesInGroup(ObjectsGroup);

		foreach (var node in objects)
		{
			if (node is not RigidBody3D body)
				continue;

			ulong instanceId = body.GetInstanceId();

			float linearSpeed = body.LinearVelocity.Length();
			float angularSpeed = body.AngularVelocity.Length();

			bool movingFast =
				linearSpeed >= MinLinearSpeed ||
				angularSpeed >= MinAngularSpeed;

			bool wasMovingFast =
				_wasMovingFast.GetValueOrDefault(instanceId);

			if (movingFast && !wasMovingFast)
			{
				float points = PointsPerEvent;

				if (linearSpeed >= HighLinearSpeed)
					points += HighSpeedBonus;

				_chaosScore += points;

				GD.Print(
					$"Chaos: {_chaosScore:F1}/{ChaosThreshold:F1}"
				);
			}

			_wasMovingFast[instanceId] = movingFast;
		}

		if (_chaosScore >= ChaosThreshold &&
			_cooldownRemaining <= 0.0)
		{
			_cooldownRemaining = ReactionCooldown;

			EmitSignal(SignalName.DestructionThresholdReached);

			_chaosScore = 0.0f;
		}
	}

}
